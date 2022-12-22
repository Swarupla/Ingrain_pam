import { Component, OnInit, ViewChild, ChangeDetectorRef, AfterContentChecked, ChangeDetectionStrategy, ElementRef, Inject, TemplateRef, EventEmitter, Output } from '@angular/core';
import { FeatureSelectionService } from 'src/app/_services/feature-selection.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { empty, throwError, timer, of } from 'rxjs';
import { tap, switchMap, catchError } from 'rxjs/operators';
import { Router, NavigationEnd } from '@angular/router';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { AppUtilsService } from 'src/app/_services/app-utils.service';

@Component({
  selector: 'app-feature-selection',
  templateUrl: './feature-selection.component.html',
  styleUrls: ['./feature-selection.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})



export class FeatureSelectionComponent implements OnInit, AfterContentChecked {
  correlationId: string;
  userId;
  pageInfo = 'DataPreprocessing';
  isLoading: boolean;
  features: {};
  trainingData: number;
  isClusteringFlag: boolean;
  featurenames = [];
  importanceValue = {};
  selectionStatus: {};
  importance: any;
  SelectedKFold: any;
  stratifiedSampling: any;
  featureForSplit: any;
  newModelName: {};
  postTrainingData: { Train_Test_Split: { TrainingData: any; }; };
  postKfoldValidation: { KFoldValidation: { ApplyKFold: any; SelectedKFold: any }; };
  postCheckedFeature = {};
  postSwitchState: { StratifiedSampling: string; };
  newCorrelationIdAfterCloned;
  featureSelectionSubscription: any;
  pythonProgressStatus: any;
  pythonProgress: any;
  timerSubscripton: any;
  noData: string;
  processData: any;
  modelName: any;
  dataSource: any;
  useCase: any;
  showhelppanel: boolean;
  statusForPrescription: boolean;
  fixFeatureImportnaceofZero: Set<string> = new Set();
  featuresNamestobeFixed: Set<string> = new Set();
  targetColumn: string;
  isTimeSeriesModelType: boolean;
  deliveryTypeName;
  modalRef: BsModalRef;
  config = {
    backdrop: true,
    class: 'deploymodle-confirmpopup'
  };
  retrainFlag = false;
  maxKfoldValidation: any;
  isAllData_Flag: boolean = false;
  readOnly;
  IsModelTrained = false;
  targetWidth: any;
  isCascadingEnabled = false;
  isFeatureSelectionSaved = true;
  isFMFlag = true;
  isIncludedInNormalCascade = false;
  targetUniqueIdentifier = '';
  isCascadeEnabledUniqueIdentifier = false;
  onSaveNextButtonDisable = false;
  decimalPoint: any;

  @Output() isNextDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();
  // payload =
  // {
  //   "_id" : "5edf2726c9d7d7ff473d8ecf",
  //   "CorrelationId" : "cd41621e-d395-4af6-b976-24c447d18746",
  //   "Actual_Target" : "smoker_L",
  //   "Cluster_Columns" : [
  //     "Cluster0",
  //     "Cluster1"
  //   ],
  //   "CreatedBy" : "systemadminui@mwphoenix.onmicrosoft.com",
  //   "FeatureImportance" : {
  //     "charges" : {
  //       "Selection" : "True",
  //       "Value" : 0.7407627966775774
  //     },
  //     "bmi" : {
  //       "Selection" : "True",
  //       "Value" : 0.09788344620332846
  //     },
  //     "age" : {
  //       "Selection" : "True",
  //       "Value" : 0.06284222238191385
  //     },

  //     "children" : {
  //       "Selection" : "True",
  //       "Value" : 0.02695860829760192
  //     },
  //     "region" : {
  //       "Selection" : "True",
  //       "Value" : 0.021156884999799234
  //     },
  //     "sex" : {
  //       "Selection" : "True",
  //       "Value" : 0.010037274517792743
  //     },

  //     "smoker" : {
  //       "Selection" : "True",
  //       "Value" : 0
  //     },
  //     "All_Text":{
  //       "Selection" : "True",
  //       "Value" : 0
  //     },

  //   },
  //   "Feature_Not_Created" : {

  //   },
  //   "Features_Created" : [ ],
  //   "Final_Text_Columns" : [
  //     "Summary"
  //   ],
  //   "Frequency_dict" : {
  //     "Summary" : {
  //       "freq_most" : {

  //       },
  //       "freq_rare" : {

  //       }
  //     }
  //   },
  //   "KFoldValidation" : {
  //     "ApplyKFold" : 10
  //   },
  //   "Map_Encode_New_Feature" : [ ],
  //   "NLP_Flag" : true,
  //   "ClusteringFlag": true,
  //   "Split_Column" : {
  //     "TargetColumn" : "smoker"
  //   },
  //   "StratifiedSampling" : "True",
  //   "Text_Null_Columns_Less20" : [ ],
  //   "Train_Test_Split" : {
  //     "TrainingData" : 70
  //   },
  //   "UniqueTarget" : [
  //     "yes",
  //     "no"
  //   ],
  //   "UniqueTarget_Message" : [
  //     "null"
  //   ],
  //   "pageInfo" : "DataPreprocessing"
  // };

  constructor(private fs: FeatureSelectionService,
    private ls: LocalStorageService, private cu: CoreUtilsService,
    @Inject(ElementRef) private eleRef: ElementRef,
    private dialogService: DialogService, private router: Router,
    private ns: NotificationService, private ps: ProblemStatementService
    , private changeDetectorRef: ChangeDetectorRef, private uts: UsageTrackingService,
    private _appUtilsService: AppUtilsService,
    private _modalService: BsModalService) {
      this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
  }

  ngOnInit() {
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.isLoading = true;
    this.statusForPrescription = true;
    this.correlationId = this.ls.getCorrelationId();
    this.uts.usageTracking('Model Engineering', 'Feature Selection');
    this.getDataOfFeatureSelection();
    let customCascadedId = sessionStorage.getItem('customCascadedId');
    if (customCascadedId !== undefined && customCascadedId !== null) {
      this.isCascadingEnabled = true;
    }
  }

  ngAfterContentChecked(): void {
    this.changeDetectorRef.detectChanges();
  }

  prescriptionOpen(event) {
    event.preventDefault();
    this.statusForPrescription = !this.statusForPrescription;
  }

  ignore(event) {
    event.preventDefault();
    this.statusForPrescription = true;
  }

  getDataOfFeatureSelection() {
    this.userId = this._appUtilsService.getCookies().UserId;
    this.featureSelectionSubscription =
      this.fs.getFeatures(this.correlationId, this.userId, this.pageInfo)
        .pipe(
          switchMap(
            data => {
              let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
              msg = msg + ' Please try again while we troubleshoot the error';
              if (this.IsJsonString(data)) {
                const parsedData = JSON.parse(data);
                if (parsedData.hasOwnProperty('message')) {
                  return this.fs.getFeatures(this.correlationId, this.userId, this.pageInfo);
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

  setAttributes(data) {

    if (this.cu.isNil(data.ProcessData)) {
      this.ns.error(`Error occurred: Due
      to some backend data process
       the relevant data could not be produced.
        Please try again while we troubleshoot the error`);
    } else {
      this.retrainFlag = data.Retrain;
      this.disableTabs();
      this.modelName = data.ModelName;
      this.dataSource = data.DataSource;
      this.useCase = data.BusinessProblem;
      this.deliveryTypeName = data.Category;

      if (data.hasOwnProperty('IsFMModel')) {
        this.isFMFlag = data.IsFMModel;
      }

      if (data.hasOwnProperty('IsIncludedInNormalCascade')) {
        this.isIncludedInNormalCascade = data.IsIncludedInNormalCascade;
      }

      if (data.hasOwnProperty('IsCascadingButton')) {
        if (this.isCascadingEnabled === true && data.IsCascadingButton === false) {
          this.isFeatureSelectionSaved = false;
        }
        if (sessionStorage.getItem('customCascadedId') === undefined || sessionStorage.getItem('customCascadedId') === null) {
          this.isCascadingEnabled = data.IsCascadingButton;
        }
      }
      if (!this.cu.isNil(data.IsModelTrained)) {
        this.IsModelTrained = data.IsModelTrained;
      }
      if (!this.cu.isNil(data.ModelType) && data.ModelType === 'TimeSeries') {
        this.isTimeSeriesModelType = true;
      } else {
        this.isTimeSeriesModelType = false;
      }
      this.isLoading = false;
      if (!this.cu.isNil(data.ProcessData)) {
        this.processData = data.ProcessData;
      }
      if (!this.cu.isNil(this.processData.FeatureImportance)) {
        this.features = this.processData.FeatureImportance;
      }
      if (!this.cu.isNil(this.processData.Clustering_Flag)) {
        this.isClusteringFlag = this.processData.Clustering_Flag;
      } else {
        this.isClusteringFlag = false;
      }
      if (!this.cu.isNil(this.processData.Train_Test_Split)) {
        this.trainingData = this.processData.Train_Test_Split.TrainingData;
      }
      if (!this.cu.isNil(this.processData.KFoldValidation)) {
        this.maxKfoldValidation = this.processData.KFoldValidation.ApplyKFold;
        if (this.processData.KFoldValidation.hasOwnProperty('SelectedKFold') && this.processData.KFoldValidation.SelectedKFold) {
          this.SelectedKFold = this.processData.KFoldValidation.SelectedKFold;
        } else {
          this.SelectedKFold = (this.maxKfoldValidation <= 5) ? 2 : (this.maxKfoldValidation / 2) + "";
          this.changedValueForValidation(this.SelectedKFold);
        }
        if (this.maxKfoldValidation === 0) {
          this.ns.error('The number of records is less than 20. Please try and upload a new file with more than 20 records.');
          this.fs.setApplyFlag(true);
          this.isNextDisabled.emit(true);
        } else {
          this.fs.setApplyFlag(false);
          this.isNextDisabled.emit(false);
        }
      }
      if (!this.cu.isNil(this.processData.StratifiedSampling)) {
        this.stratifiedSampling = this.processData.StratifiedSampling;
      }
      if (!this.cu.isNil(this.processData.Split_Column)) {
        this.featureForSplit = this.processData.Split_Column.TargetColumn;
        this.targetWidth = this.featureForSplit.length * 8;
      }
      if (!this.cu.isNil(this.processData.AllData_Flag)) {
        this.isAllData_Flag = this.processData.AllData_Flag;
      }

      this.isClusteringFlag = this.processData.ClusteringFlag;
      if (data.hasOwnProperty('TargetUniqueIdentifier')) {
        this.targetUniqueIdentifier = data.TargetUniqueIdentifier;
      }
      if (this.isCascadingEnabled && this.features[this.targetUniqueIdentifier].Selection === 'False') {
        this.isNextDisabled.emit(true);
      }
      this.getFeatureImportance();
    }
    this.unsubscribe();

  }

  IsJsonString(str) {
    try {
      JSON.parse(str);
    } catch (e) {
      return false;
    }
    return true;
  }

  retry() {
    this.timerSubscripton = timer(10000).subscribe(() => this.getDataOfFeatureSelection());
    return this.timerSubscripton;
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

  onChecked(elementRef) {
    if (elementRef.checked === true) {
      this.featuresNamestobeFixed.add(elementRef.value);
    }
  }

  fixIt() {
    if (this.featuresNamestobeFixed.size > 0) {
      this.fs.fixFeatures(this.correlationId, this.featuresNamestobeFixed).subscribe(
        data => {
          this.ns.success('Data Fixed Successfully.');
          window.location.reload();
        }
      );
    } else {
      this.ns.error('No Feature(s) selected to fix');
    }
  }

  //  getStyles(value) {
  //   const styles = {
  //     'position': 'absolute',
  //     'left': value - 60 + '%',
  //     'z-index': '1'
  //   };
  //   return styles;
  // }

  // getStylesForValidation(value) {
  //   const styles = {
  //     'position': 'absolute',
  //     'left': value + '%',
  //     'z-index': '1'
  //   };
  //   return styles;
  // }

  getStyles(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 5;
    left = ((rangefromMinValue / diff) * 100);
    if (left > 88) {
      left = left - 5;
    }
    if (left < 5) {
      left = left + 5;
    }

    if (number === minValue) {
      left = 5;
    }
    if (number === maxValue) {
      left = 93;
    }
    const styles = {
      'position': 'absolute',
      'left': left + '%',
      'z-index': '1',
      'top': '5.8em',
      'color': (!this.isAllData_Flag) ? '#000' : 'rgb(171, 166, 211)',
      'font-size': '.625rem',
      'font-weight': '400'
    };
    return styles;
  }

  getStylesKfold(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 4;
    left = ((rangefromMinValue / diff) * 100);
    if (left > 88) {
      left = left - 4;
    }
    if (left < 4) {
      left = left + 4;
    }

    if (number === minValue) {
      left = 4;
    }
    if (number === maxValue) {
      left = 93;
    }
    const styles = {
      'position': 'absolute',
      'left': left + '%',
      'z-index': '1',
      'top': '5.8em',
      'color': (!this.isAllData_Flag) ? '#000' : 'rgb(171, 166, 211)',
      'font-size': '.625rem',
      'font-weight': '400'
    };
    return styles;
  }

  changedValueForTrainingData(value) {
    this.trainingData = value;
    this.postTrainingData = {
      Train_Test_Split: {
        TrainingData: value
      }
    };
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


  onCheckChanged(key, elementRef) {
    if (elementRef.checked === true) {
      this.features[key].Selection = 'True'
      this.postCheckedFeature[key] = {};
      this.postCheckedFeature[key] = {
        Selection: 'True'
      };
      if (this.isCascadingEnabled) {
        this.isCascadeEnabledUniqueIdentifier = true;
        this.isNextDisabled.emit(false);
      }
    } else if (elementRef.checked === false) {
      this.features[key].Selection = 'False'
      this.postCheckedFeature[key] = {};
      this.postCheckedFeature[key] = {
        Selection: 'False'
      };
    }
  }

  onSwitchChanged(elementRef) {
    if (elementRef.checked === true) {
      this.postSwitchState = {
        StratifiedSampling: 'True'
      };
    }
    if (elementRef.checked === false) {
      this.postSwitchState = {
        StratifiedSampling: 'False'
      };
    }
  }


  checkRetrainStatus(confirmChangesOnFeatureSelection?: TemplateRef<any>) {
    if (this.IsModelTrained !== undefined) {
      if (this.IsModelTrained === true) {
        this.modalRef = this._modalService.show(confirmChangesOnFeatureSelection, this.config);
      } else {
        this.postSelectedFeatures('');
      }
    } else {
      this.postSelectedFeatures('');
    }
  }

  confirm() {
    this.modalRef.hide();
    this.postSelectedFeatures('');
  }
  // confirmation popup cancle event
  decline() {
    this.modalRef.hide();
  }

  postSelectedFeatures($event) {
    if (!this.readOnly) {
      this.fs.postFeatures(this.correlationId, this.postCheckedFeature, this.postTrainingData,
        this.postKfoldValidation, this.postSwitchState, this.isAllData_Flag, this.isCascadingEnabled).subscribe(
          data => {
            this.ns.success('Data Saved Successfully.');
            this.isFeatureSelectionSaved = true;
            this.ngOnInit();
          });
    }
  }

  saveAs($event) {

    const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this.ps.clone(this.correlationId, modelName);
        }
        // tslint:disable-next-line: deprecation
        return empty();
      }),

      tap(data => this.newCorrelationIdAfterCloned = data),

      tap(data => {

        if (data) {
          this.router.navigate(['dashboard/modelengineering/FeatureSelection'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });

          this.ns.success('Cloned Successfully');
        }
      })
    );

    openTemplateAfterClosed.subscribe();
  }


  private disableTabs() {
    // this.ps.getColumnsdata.IsModelTrained
    const classnameDisabled = 'anchor-disabled';
    const classnameEnabled = 'anchor-enabled';
    // this.des.setApplyFlag(false);
    const nativeElemnt = !this.cu.isNil(this.eleRef.nativeElement) ? this.eleRef.nativeElement : null;
    const parentEle = !this.cu.isNil(nativeElemnt) ? nativeElemnt.parentElement : null;
    const nestedParentEle = !this.cu.isNil(parentEle) ? parentEle.parentElement : null;
    if (!this.cu.isNil(nestedParentEle)) {
      const allLinks = nestedParentEle.children[0].querySelectorAll('a');
      const submenulinks = nestedParentEle.children[1].querySelectorAll('a');


      for (let index = 0; index < submenulinks.length; index++) {

        // if (this.ps.getColumnsdata.IsModelTrained) {
        //   if (submenulinks[index].text === 'Teach and Test' || submenulinks[index].text === 'COMPARE TEST SCENARIOS') {
        //     if (this.retrainFlag) {
        //       submenulinks[index].className = 'ingrAI-sub-nav-button ' + classnameDisabled;
        //     } else {
        //       submenulinks[index].className = 'ingrAI-sub-nav-button ' + classnameEnabled;
        //     }
        //   }
        // } else if (submenulinks[index].text === 'Teach and Test' || submenulinks[index].text === 'COMPARE TEST SCENARIOS') {
        //   submenulinks[index].className = 'ingrAI-sub-nav-button ' + classnameDisabled;
        // }
        if (submenulinks[index].text === 'Teach and Test' || submenulinks[index].text === 'COMPARE TEST SCENARIOS') {
          submenulinks[index].className = 'myw-step ' + classnameDisabled;
        }
      }
    }
  }

  AllData_FlagChecked(elementRef) {
    this.isAllData_Flag = elementRef.checked === true;
  }

  // cascadeSwitchToggle(e) {
  //   this.isFeatureSelectionSaved = false;
  //   this.isCascadingEnabled = e.target.checked;
  // }

  cascadeSwitchToggle(e) {
    this.isFeatureSelectionSaved = false;
    this.isCascadingEnabled = e.target.checked;
    if (this.isCascadingEnabled) {

      if (this.features[this.targetUniqueIdentifier].Selection === 'False') {
        this.isNextDisabled.emit(true);
        this.ns.warning('Unique Identifier is required if model is to be considered for Cascading process');
        this.isCascadeEnabledUniqueIdentifier = false;
      } else {       
        this.postCheckedFeature[this.targetUniqueIdentifier] = {};
        this.postCheckedFeature[this.targetUniqueIdentifier] = {
          Selection: 'True'
        };
        this.isCascadeEnabledUniqueIdentifier = true;
      }
     // console.log('-Featureselection ----', this.features[this.targetUniqueIdentifier].Selection, '----------', this.postCheckedFeature[this.targetUniqueIdentifier])
    } else {
      this.isCascadeEnabledUniqueIdentifier = false;
      this.isNextDisabled.emit(false);
    }
  }
}
