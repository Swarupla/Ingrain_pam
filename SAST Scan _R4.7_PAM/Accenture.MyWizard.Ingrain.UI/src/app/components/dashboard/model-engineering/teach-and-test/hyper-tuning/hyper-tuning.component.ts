import { Component, OnInit, Inject, ElementRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TeachTestService } from 'src/app/_services/teach-test.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { Subscription, timer, of, empty, throwError } from 'rxjs';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { CookieService } from 'ngx-cookie-service';
import { HyperTuningService } from 'src/app/_services/hyper-tuning.service';
import { tap, switchMap, catchError } from 'rxjs/operators';
import { DomSanitizer } from '@angular/platform-browser';
import { DialogService } from 'src/app/dialog/dialog.service';
import {
  HtVersionNameComponent
} from './ht-version-name/ht-version-name.component';

@Component({
  selector: 'app-hyper-tuning',
  templateUrl: './hyper-tuning.component.html',
  styleUrls: ['./hyper-tuning.component.scss']
})
export class HyperTuningComponent implements OnInit {
  data = {};
  HyperTuningSettingsDropDownOptions = [];
  HyperParameters: string[] = [];
  isLoading: boolean;
  FloatHyperParameters = {
    'keys': []
  };
  cpuUsageValue = 0;
  memoryUsageInMB: number;
  isTrainingBtn: boolean;
  saveVersionButton: boolean;

  correlationId: string;
  timerSubscripton: Subscription;
  hyperTuningSubscription: Subscription;
  isTrainingStart: boolean;
  selectedModelNameforCharts: string;
  modelAccuracy;
  img_64: string;
  f1Score: number;
  selectedModelF1Score: number;
  falseNegative: number;
  falsePositive: number;
  trueNegative: number;
  truePositive: number;
  pythonProgressVal: number;
  ROCAUCvalue = {};
  selectedModelROCAUCvalue: number;
  featureWeight = {};
  selectedModelFeatureWeight = {};
  pythonProgress;
  userIdForTrainedModels: string;
  pageInfoForTrainedModels = 'HyperTune';
  selectedFineTuneImg_64;
  selectedModelFalseNegative: number;
  selectedModelTrueNegative: number;
  selectedModelFalsePositive: number;
  selectedModelTruePositive: number;
  totalOfTrueFalsePositive: number;
  totalofTrueFalseNegative: number;
  totalofTruePositiveFalseNegative: number;
  totalofFalsePositiveTrueNegative: number;
  sumofTrueFalseValues: number;
  selectedModelName;
  noData: any;
  r2Values: number;
  mseValues: number;
  isClassificationAccuracy: boolean;
  isRegressionR2Val: boolean;
  isMultiClass: boolean;
  defaultModelAccuracyOnLoad: number;
  selectedAccuracyValue: number;
  isModelSaved: boolean;
  savedHypertunedData: any;
  showSavedVersion: boolean;
  savedValues;
  hyperTuneId: string;
  HTId: string;
  dataToServer = {};
  IntegerHyperParameters = {
    'keys': []
  };
  selectedModelRuntime: string;
  classificationReport = {};
  matthewsCoefficient: number;
  confustionMatirxImg_64: string;
  selectedMatthewsCoefficient: number;
  reportTblHeading = [];
  reportColVal = [];
  reportCollenght = [];
  multiClassConfusionMatrix;
  isTextPreProcessing: boolean;
  showTextPreProcessDiv: boolean;
  clusteringFlag: boolean = true;
  readOnly;

  constructor(private _route: ActivatedRoute, private ts: TeachTestService,
    private _localStorageService: LocalStorageService,
    private _notificationService: NotificationService,
    private _appUtilsService: AppUtilsService, private _cookieService: CookieService,
    private _hyperTuningService: HyperTuningService,
    private domSanitizer: DomSanitizer,
    private dialogService: DialogService, private coreUtilsService: CoreUtilsService,
    @Inject(ElementRef) private eleRef: ElementRef, private _router: Router,
  ) { }

  ngOnInit() {
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.isTrainingBtn = true;
    this.pythonProgressVal = 0;
    this._appUtilsService.loadingStarted();
    this.correlationId = this._localStorageService.getCorrelationId();
    // this.correlationId = '9c420fd6-b266-4247-a4a2-d7b0dddc9318';
    this.userIdForTrainedModels = this._appUtilsService.getCookies().UserId;
    this.selectedModelName = localStorage.getItem('SelectedRecommendedModel'); // this._cookieService.get('SelectedRecommendedModel');
    this.selectedModelRuntime = localStorage.getItem('SelectedModelRunTime');
    this.showSavedVersion = false;
    this._hyperTuningService.getHyperTuningData(this.selectedModelName, this.correlationId).subscribe(
      data => {
        if (data instanceof Object && Object.keys(data).length > 0) {
          this.isLoading = true;
          this.data = data;
          this.setHyperTuningData(data);
        }
        this._appUtilsService.loadingEnded();
      }, error => {
        this._appUtilsService.loadingEnded();
        this._notificationService.error(' Something went wrong to getHyperTuningData.');
      }
    );
  }


  setHyperTuningData(data) {
    this.setHyperTuningDropDownData(data);
    this.setSlidersData(data);
    this.setDataForDropDownsinTable(data);
    this.setIntergersData(data);
    this.isLoading = false;
  }

  setIntergersData(data: any) {
    if (data.hasOwnProperty('IntegerHyperParameters')) {

      if (data['IntegerHyperParameters'] instanceof Array) {

        data['IntegerHyperParameters'].forEach(element => {
          this.IntegerHyperParameters['keys'].push(element['AttributeName']);
          element['checkedstatus'] = true;
          this.IntegerHyperParameters[element['AttributeName']] = element;
          this.dataToServer[element['AttributeName']] = element.DefaultValue;
        });
      }
    }
  }

  setDataForDropDownsinTable(data) {
    if (data.hasOwnProperty('StringHyperParameters')) {
      if (data['StringHyperParameters'] instanceof Object) {
        this.HyperParameters = Object.keys(data['StringHyperParameters']);
        this.HyperParameters.forEach(parameter => {
          if (this.getTrueValue(data['StringHyperParameters'][parameter])) {
            data['StringHyperParameters'][parameter] = this.getTrueValue(data['StringHyperParameters'][parameter]);
          }
          data['StringHyperParameters'][parameter]['keys'] = Object.keys(data['StringHyperParameters'][parameter]);
        });
      }
    }
  }


  getTrueValue(data) {
    Object.keys(data).forEach(el => {
      if (data[el] === 'true') {
        return data[el];
      }
    });

    return false;
  }
  setSlidersData(data) {
    if (data.hasOwnProperty('FloatHyperParameters')) {

      if (data['FloatHyperParameters'] instanceof Array) {

        data['FloatHyperParameters'].forEach(element => {
          this.FloatHyperParameters['keys'].push(element['AttributeName']);
          element['checkedstatus'] = true;
          this.FloatHyperParameters[element['AttributeName']] = element;
          this.dataToServer[element['AttributeName']] = element.DefaultValue;
        });
      }
    }
  }

  onChange(checkBox, row, value, paramType) {

    if (this.FloatHyperParameters[row]) {
      this.FloatHyperParameters[row]['DefaultValue'] = value;
    }
    if (this.IntegerHyperParameters[row]) {
      this.IntegerHyperParameters[row]['DefaultValue'] = value;
    }

    if (checkBox.checked === false) {
      delete this.dataToServer[row];
    }
    if (checkBox.checked === true) {
      if (value === '') {
        delete this.dataToServer[row];
      } else {

        if (paramType === 'FloatParam') {
          this.dataToServer[row] = +value;
        } else if (paramType === 'StringParam') {
          this.dataToServer[row] = value;
        } else if (paramType === 'IntegerParam') {
          this.dataToServer[row] = +value;
        }
      }
    }
  }

  onStringDropdownChange(value, row) {

    this.HyperParameters[row] = 'True';
  }

  setHyperTuningDropDownData(data) {

    if (data.hasOwnProperty('SavedHyperVersions')) {

      if (data['SavedHyperVersions'] instanceof Array) {
        if (data['SavedHyperVersions'].length > 0) {
          data['SavedHyperVersions'].forEach(obj => {
            const versionName = obj['VersionName'];
            this.HyperTuningSettingsDropDownOptions.push(versionName);
          });

        }
      }

    }
  }

  saveHyperTuningParamForTraining() {
    this._hyperTuningService.postHyperTuning(this.correlationId, this.dataToServer).subscribe(response => {
      if (response) {
        this.hyperTuneId = response;
        this.HTId = response;
        if (!this.isModelSaved) {

        }
        this.isModelSaved = true;
        this.startTraining();
      } else {
        this._notificationService.error('Something went wrong to saveHyperTuningParamForTraining');
      }
    }, error => {
      this._notificationService.error('Something went wrong to saveHyperTuningParamForTraining');
    });
  }
  onChangeOfHypertuneVersion(event) {
    const selectedversion = event.target.value;
    this._appUtilsService.loadingStarted();
    if (selectedversion !== 'savedVersion') {
      let hypertunedata = this.data['SavedHyperVersions'];
      hypertunedata = hypertunedata.find(version => version.VersionName === selectedversion);
      this.ts.GetHyperTunedDataByVersion(hypertunedata).subscribe(
        data => {
          if (data instanceof Object && Object.keys(data).length > 0) {
            if (!this.coreUtilsService.isNil(data['TrainedModel']) && data['TrainedModel'].length > 0) {
              this.savedHypertunedData = data['TrainedModel'][0].ModelParams;
              this.showSavedVersion = true;
              this.isTrainingBtn = false;
              this.saveVersionButton = false;
              this.setAttributes(data);
            }
          }
          this._appUtilsService.loadingEnded();
        },
        error => {
          this._notificationService.error('Something went wrong.');
          this._appUtilsService.loadingEnded();
        }
      );
    } else {
      this._appUtilsService.loadingEnded();
      this.isTrainingBtn = true;
      this.setClassOnElement('dvAfterTrainingDetails', 'ingrAI-box-container mt-4 ingrAI-hyper-tuning d-none');
      this.showSavedVersion = false;
    }
  }
  openVersionNamePopup() {
    const openTemplateAfterClosed =
      this.dialogService.open(HtVersionNameComponent, { data: { title: 'Save Hyper Tuning Setting' } }).afterClosed.pipe(
        tap(data => data ? this.saveHyperTuningSetting(data) : ''));

    openTemplateAfterClosed.subscribe();

  }
  saveHyperTuningSetting(versionName) {
    const HTId = this.HTId;
    const temp = 'false';
    this._appUtilsService.loadingStarted();
    this._hyperTuningService.saveHyperTuningVersion(this.correlationId, HTId, temp, versionName).subscribe(response => {
      let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
      msg = msg + ' Please try again while we troubleshoot the error.';
      if (response != null) {
        if (this.isModelSaved) {
          this.isModelSaved = false;
        }
        this.setClassOnElement('dvAfterTrainingDetails', 'ingrAI-box-container mt-4 ingrAI-hyper-tuning d-none');
        this._notificationService.success('Hypertuning Version is saved successfully');
        this.saveVersionButton = false;
        this.data['SavedHyperVersions'] = [];
        this.data['SavedHyperVersions'] = response['HyperTunedVersionData'];
        this.setHyperTuningDropDownDataAfterSave(response);
        this.isTrainingBtn = true;
        this.saveVersionButton = false;
      } else {
        this._notificationService.error(msg);
      }
      this._appUtilsService.loadingEnded();
    }, error => {
      this._appUtilsService.loadingEnded();
      let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
      msg = msg + ' Please try again while we troubleshoot the error.';
      this._notificationService.error(msg);
      this.saveVersionButton = false;
    });
  }

  setHyperTuningDropDownDataAfterSave(data) {
    if (data.hasOwnProperty('HyperTunedVersionData')) {

      if (data['HyperTunedVersionData'] instanceof Array) {
        if (data['HyperTunedVersionData'].length > 0) {
          this.HyperTuningSettingsDropDownOptions = [];
          data['HyperTunedVersionData'].forEach(obj => {
            const versionName = obj['VersionName'];
            this.HyperTuningSettingsDropDownOptions.push(versionName);
          });
        }
      }

    }
  }

  startTraining() {
    this.isTrainingBtn = false;
    if (Object.keys(this.dataToServer).length !== 0) {
      if (!this.isModelSaved) {
        this.saveHyperTuningParamForTraining();
      }
      this.setClassOnElement('dvAfterTrainingDetails', 'ingrAI-box-container mt-4 ingrAI-hyper-tuning d-none');
      // this.hyperTuneId = '954e6099-9399-4a16-b7cb-2f28ea683e76'
      if (this.hyperTuneId === undefined || this.hyperTuneId === null) {
        this.isTrainingBtn = true;
      } else {
        this._hyperTuningService.hyperTuningStartTrainring(this.correlationId,
          this.hyperTuneId, this.selectedModelName, this.userIdForTrainedModels, this.pageInfoForTrainedModels)
          .pipe(
            switchMap(
              data => {
                let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
                msg = msg + ' Please try again while we troubleshoot the error';
                if (data !== '' || data !== null) {
                  if (this.IsJsonString(data)) {
                    const parsedData = JSON.parse(data);
                    if (parsedData.hasOwnProperty('message')) {
                      if (parsedData.message === 'Success..' || parsedData.message === 'Request is in Progress') {
                        return this._hyperTuningService.hyperTuningStartTrainring(this.correlationId, this.hyperTuneId,
                          this.selectedModelName, this.userIdForTrainedModels, this.pageInfoForTrainedModels);
                      }
                    } else if (parsedData.hasOwnProperty('UseCaseList')) {
                      return of(parsedData);
                    } else {
                      this.noData = 'Format from server is not Recognised';

                      this._notificationService.error(msg);
                      this.unsubscribe();
                      // tslint:disable-next-line: deprecation
                      return empty();
                    }
                  } else if (data.constructor === Object) {
                    if (data.Message === 'In - Progress') {
                      return this._hyperTuningService.hyperTuningStartTrainring(this.correlationId, this.hyperTuneId,
                        this.selectedModelName, this.userIdForTrainedModels, this.pageInfoForTrainedModels);
                    }
                    this.setAttributes(data);
                    if (data.Status === 'C') {
                      this.hyperTuneId = null;
                      this.pythonProgressVal = data.Progress;;
                      this.isTrainingBtn = true;
                      this.saveVersionButton = true;
                      this.isModelSaved = false;
                    }
                    // tslint:disable-next-line: deprecation
                    return empty();
                  } else if (data.constructor === String) {
                    if (data === '') {
                      this._notificationService.error(msg);
                    } else if (data === 'bc') {
                      this._notificationService.error(msg);
                    } else {
                      this.noData = data;
                      this._notificationService.success(data);
                    }
                    this.unsubscribe();
                    // tslint:disable-next-line: deprecation
                    return empty();
                  } else {
                    this.noData = 'Format from server is not Recognised';
                    this._notificationService.error(msg);
                    this.unsubscribe();
                  }
                } else {
                  this._notificationService.error(msg);
                  this.unsubscribe();
                }
              }
            ),
            catchError(error => {
              return throwError(error);
            })
          ).subscribe(data => {
            if (this.IsJsonString(data)) {
              const responseData = JSON.parse(data);
              if (responseData.hasOwnProperty('message')) {
                if (responseData.message === 'Success..' || responseData.message === 'Request is in Progress') {
                  this.retry();
                }
              }
            } else if (data.constructor === Object) {
              this.setAttributes(data);
              if (data.Status === 'C') {
                this.hyperTuneId = null;
                this.saveVersionButton = true;
                this.pythonProgressVal = data.Progress;
                this.isTrainingBtn = true;
                this.isModelSaved = false;
              }
            }
          },
          (error) => {
            // if (resAI.process === 0 && resAI.Status === 'E') {
              this.hyperTuneId = null;
              console.log(error);
              this.pythonProgressVal = 0;
              this.saveVersionButton = false;
              this.isTrainingBtn = true;
              this.isModelSaved = false;
              let msg = 'Please change the Hyperparameters and try again';
              this._notificationService.error(msg)
          });
      }
    } else {
      this._notificationService.error('Please Select values');
      this.isTrainingBtn = true;
    }
  }

  cpuUsageInTrainning(resAI) {
    if (resAI.CPUUsage === null || resAI.CPUUsage === undefined) { } else {
      this.cpuUsageValue = Math.round(resAI.CPUUsage * 100) / 100;
    }
  }

  memoryUsageInTrainning(resAI) {
    if (resAI.MemoryUsageInMB === null || resAI.MemoryUsageInMB === undefined) { } else {
      this.memoryUsageInMB = Math.round(resAI.MemoryUsageInMB * 100) / 100;
    }
  }

  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }


  setAttributes(resAI) {

    this.cpuUsageInTrainning(resAI);
    this.memoryUsageInTrainning(resAI);

    if (resAI.TrainedModel === null) {
    } else {
      if (resAI.TrainedModel[0].ProblemType === 'classification' || resAI.TrainedModel[0].ProblemType === 'Classification') {
        this.selectedModelNameforCharts = resAI.TrainedModel[0].ProblemType;
        this.isClassificationAccuracy = true;
        this.isRegressionR2Val = false;
        this.isMultiClass = false;
        this.showTextPreProcessDiv = false;
        this.modelAccuracy = Math.round(resAI.TrainedModel[0].Accuracy * 100) / 100;

        this.img_64 = resAI.TrainedModel[0].AUCImage;
        this.f1Score = Math.round(resAI.TrainedModel[0].f1score * 100) / 100;

        this.falseNegative = resAI.TrainedModel[0].FalseNegative;
        this.falsePositive = resAI.TrainedModel[0].FalsePositive;
        this.trueNegative = resAI.TrainedModel[0].TrueNegative;
        this.truePositive = resAI.TrainedModel[0].TruePositive;

      } else if (resAI.TrainedModel[0].ProblemType === 'regression' || resAI.TrainedModel[0].ProblemType === 'Regression') {
        this.selectedModelNameforCharts = resAI.TrainedModel[0].ProblemType;

        this.isClassificationAccuracy = false;
        this.isRegressionR2Val = true;
        this.isMultiClass = false;
        this.showTextPreProcessDiv = false;

        this.modelAccuracy = resAI.TrainedModel[0].r2ScoreVal.error_rate;

        this.r2Values = resAI.TrainedModel[0].r2ScoreVal.error_rate;
        this.mseValues = Math.round(resAI.TrainedModel[0].mseVal.error_rate * 100) / 100;
      } else if (resAI.TrainedModel[0].ProblemType === 'Multi_Class') {
        this.selectedModelNameforCharts = resAI.TrainedModel[0].ProblemType;
        this.isMultiClass = true;
        this.isClassificationAccuracy = false;
        this.isRegressionR2Val = false;
        this.showTextPreProcessDiv = false;

        this.modelAccuracy = Math.round(resAI.TrainedModel[0].Accuracy * 100) / 100;
        this.classificationReport = resAI.TrainedModel[0].report;
        this.matthewsCoefficient = Math.round(resAI.TrainedModel[0].Matthews_Coefficient * 100) / 100;
        this.confustionMatirxImg_64 = resAI.TrainedModel[0].ConfusionEncoded;
      } else if (resAI.TrainedModel[0].ProblemType === 'Text_Classification') {

        this.selectedModelNameforCharts = resAI.TrainedModel[0].ProblemType;
        this.isMultiClass = false;
        this.isClassificationAccuracy = false;
        this.isRegressionR2Val = false;
        this.showTextPreProcessDiv = true;

        this.modelAccuracy = Math.round(resAI.TrainedModel[0].Accuracy * 100) / 100;
        if (resAI.TrainedModel[0].ProblemTypeFlag === false) {
          this.isTextPreProcessing = false;
          this.img_64 = resAI.TrainedModel[0].AUCImage;
          this.f1Score = Math.round(resAI.TrainedModel[0].f1score * 100) / 100;

          this.falseNegative = resAI.TrainedModel[0].FalseNegative;
          this.falsePositive = resAI.TrainedModel[0].FalsePositive;
          this.trueNegative = resAI.TrainedModel[0].TrueNegative;
          this.truePositive = resAI.TrainedModel[0].TruePositive;
          this.ROCAUCvalue = Math.round(resAI.TrainedModel[0].ar_score * 100) / 100;
        }
        if (resAI.TrainedModel[0].ProblemTypeFlag === true) {
          this.isTextPreProcessing = true;
          this.matthewsCoefficient = Math.round(resAI.TrainedModel[0].Matthews_Coefficient * 100) / 100;
        }
      }
      this.featureWeight = resAI.TrainedModel[0].featureImportance;
      this.clusteringFlag = resAI.TrainedModel[0].Clustering_Flag;

      this.setDefaultModelSelected();
      this.setClassOnElement('dvAfterTrainingDetails', 'ingrAI-box-container mt-4 ingrAI-hyper-tuning');
    }
    if (resAI.Progress <= 98 && resAI.Status === 'P') {
      this.pythonProgressVal = resAI.Progress;
      this.cpuUsageInTrainning(resAI);
      this.memoryUsageInTrainning(resAI);

      this.retry();

      this.pythonProgressVal = resAI.Progress;
      this.cpuUsageInTrainning(resAI);
      this.memoryUsageInTrainning(resAI);
    } else if (resAI.Progress >= 98 && resAI.Status === 'P') {
      this.pythonProgressVal = resAI.Progress;

      this.cpuUsageInTrainning(resAI);
      this.memoryUsageInTrainning(resAI);

    } else if (resAI.process === 0 && resAI.Status === 'E') {
      this.saveVersionButton = false;
      this.isTrainingBtn = true;
      let msg = 'Please change the Hyperparameters and try again';
      // msg = msg + ' Please try again while we troubleshoot the error';
    } else if (resAI.Progress === 100 && resAI.Status === 'C') {
      this.setAttributes(resAI);
      this.hyperTuneId = null;
      this.pythonProgressVal = 0;
      this.isTrainingBtn = true;
      this.saveVersionButton = true;
      this.isModelSaved = false;
      this.unsubscribe();
    }
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
    this.timerSubscripton = timer(5000).subscribe(() => {
      this.startTraining();
    });
    return this.timerSubscripton;
  }

  getFeatureWeight() {
    Object.entries(this.featureWeight).forEach(
      ([key, value]) => {
        this.selectedModelFeatureWeight[key] = Number(value).toFixed(3);
      }
    );
  }

  getFineTuneBase64Image(selectedAccuracy) {
    if (this.img_64) {
      this.selectedFineTuneImg_64 = this.img_64.substring(2).slice(0, -1);
      this.selectedFineTuneImg_64 = 'data:image/png;base64,' + this.selectedFineTuneImg_64;
    }
  }

  generateConfusionMatrix() {

    this.totalOfTrueFalsePositive = this.falsePositive + this.truePositive;
    this.totalofTrueFalseNegative = this.falseNegative + this.trueNegative;

    this.totalofTruePositiveFalseNegative = this.truePositive + this.falseNegative;
    this.totalofFalsePositiveTrueNegative = this.falsePositive + this.trueNegative;
    this.sumofTrueFalseValues = this.falsePositive + this.truePositive + this.falseNegative + this.trueNegative;
  }

  setClassOnElement(elementId: string, className: string) {
    this.eleRef.nativeElement.parentElement.querySelector('#' + elementId).className = className;
  }

  setDefaultModelSelected() {
    let accuracy = 0;
    accuracy = this.modelAccuracy;

    this.defaultModelAccuracyOnLoad = accuracy;
    this.selectedAccuracyValue = this.defaultModelAccuracyOnLoad;

    if (this.selectedModelNameforCharts !== 'Text_Classification' &&
      this.featureWeight !== null &&
      this.clusteringFlag !== false) {
      this.getFeatureWeight();
    }

    if (this.isMultiClass || this.isTextPreProcessing) {
      this.getMatthewsCoefficient();
      if (this.isMultiClass) {
        this.getFormatedConfusionMatirxImg();
        this.getClassificationReport();
        this.generateConfusionMatrix();
      }
    } else {
      this.getFineTuneBase64Image(this.defaultModelAccuracyOnLoad);
      this.generateConfusionMatrix();
    }
  }

  getFormatedConfusionMatirxImg() {
    if (this.confustionMatirxImg_64) {
      this.multiClassConfusionMatrix = this.confustionMatirxImg_64.substring(2).slice(0, -1);
      this.multiClassConfusionMatrix = 'data:image/png;base64,' + this.multiClassConfusionMatrix;
    }
  }

  getClassificationReport() {
    const a = [];
    let clsColumnValues = {};
    let reportTargetName = {};
    reportTargetName = Object.keys(this.classificationReport);
    clsColumnValues = Object.values(this.classificationReport);

    const colLenght = Object.entries(clsColumnValues).length;
    for (let cls = 0; cls < colLenght; cls++) {
      this.reportTblHeading = Object.keys(clsColumnValues[cls]);
      this.reportColVal[cls] = Object.values(clsColumnValues[cls]);
    }

    for (let c = 0; c < this.reportColVal.length; c++) {
      a[c] = this.reportColVal[c].unshift(reportTargetName[c]);
      this.reportCollenght = this.reportColVal[0];
    }
  }

  getMatthewsCoefficient() {
    this.selectedMatthewsCoefficient = this.matthewsCoefficient;
  }


  getStyles(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 5;
    left = ((rangefromMinValue / diff) * 100);
    /* if (left > 83) {
      left = left - 13;
    } */
    if (left < 5) {
      left = left + 5;
    }

    if (number === minValue) {
      left = 5;
    }
    /* if (number === maxValue) {
      left = 91;
    } */
    if (rangefromMinValue === 0) { // For Min, Max, Default are 0
      left = 5;
    }

    const styles = {
      'position': 'absolute',
      'left': left + '%',
      'z-index': '1',
      'color': '#000',
      'font-size': '.625rem',
      // 'font-weight': '700'
    };
    return styles;
  }

  accessDenied() {
    this._notificationService.error('Access Denied');
  }

  teachAndTestToggle(event) {
    let requiredUrl = 'dashboard/modelengineering/TeachAndTest/HyperTuning';
    if (event.currentTarget.checked === false) {
      requiredUrl = 'dashboard/modelengineering/TeachAndTest/WhatIfAnalysis';
    }
    this._router.navigateByUrl(requiredUrl);
  }

}
