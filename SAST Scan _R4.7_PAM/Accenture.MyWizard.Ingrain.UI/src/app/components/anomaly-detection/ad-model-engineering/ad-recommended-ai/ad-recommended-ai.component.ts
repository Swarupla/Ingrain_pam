import { AfterViewInit, Component, ElementRef, EventEmitter, Inject, OnChanges, OnInit, Output, TemplateRef } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { CookieService } from 'ngx-cookie-service';
import { empty, of, throwError, timer } from 'rxjs';
import { Subscription } from 'rxjs/internal/Subscription';
import { catchError, tap, switchMap } from 'rxjs/operators';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { TrainedModels } from 'src/app/_models/User';
import { AdRecommendedService } from 'src/app/_services/ad-recommended.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { FeatureSelectionService } from 'src/app/_services/feature-selection.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';

@Component({
  selector: 'app-ad-recommended-ai',
  templateUrl: './ad-recommended-ai.component.html',
  styleUrls: ['./ad-recommended-ai.component.scss']
})
export class AdRecommendedAiComponent implements OnInit, OnChanges, AfterViewInit {

  @Output() isTrainingstarted : EventEmitter<boolean> ;

  lineChartData : any = [];
  barChartData: any = [];
  trainedModels: Array<TrainedModels> = [];
  correlationId: string;
  searchText;
  pstModelName: string;
  pstDataSourceName: string;
  pstUseCase: string;
  isTrainingStart = false;
  isModelTrained = true;
  donutInnerRadius: number;
  pageInfoForDonutChart: string;
  chartContainerInfo: string;

  recommendedModelList;
  recommendedModelName = [];
  recommendedModelNameStatus = [];

  postTrainModelStatus: {};
  isClusteringFlag = Boolean;
  newModelName: string;
  newCorrelationIdAfterCloned: string;
  pythonProgress;
  recommendedtrainedModels;
  FinaltrainedModels = [];
  recommendedtrainedModelNames = [];

  postStartTrainningData = {
    'SelectedModels': {}
  };
  selectedAccuracyValue: number;
  selectedModelOnLoad: string;
  isRegressionR2Val: boolean;
  isTimeSeriesVal: boolean;

  isRetrainButton: boolean;
  featureWeight = {};
  lineChart = [];
  timerSubscripton: Subscription;
  recommendedAiSubscription: Subscription;
  noData: string;
  selectedModelF1Score: number;

  selectedModelr2Values: number;
  selectedModelMaeValues: number;

  prescriptionStatus = false;
  prescriptionTemplateText: string;
  selectedTrainedModelName: string;
  selectedModelRunTimeValue: string;
  timeSeriesAIShow = false;
  regressionAIShow = false;
  consusionMatrix: [];
  selectedFineTuneImg_64;

  userIdForTrainedModels: string;
  pageInfoForTrainedModels = 'RecommendedAI';
  recommendedModelNamesBeforeTraining = [];
  availableModels;
  runTimeOfRecommendedModels = {};
  isModelChecked = {};
  chartAccuracy: any;
  defaultModelAccuracyOnLoad: number;
  falseNegative = {};
  falsePositive = {};
  trueNegative = {};
  truePositive = {};
  pythonProgressVal = 0;
  selectedModelNameforCharts: string;
  selectedModelFalseNegative: number;
  selectedModelTrueNegative: number;
  selectedModelFalsePositive: number;
  selectedModelTruePositive: number;
  totalOfTrueFalsePositive: number;
  totalofTrueFalseNegative: number;
  totalofTruePositiveFalseNegative: number;
  totalofFalsePositiveTrueNegative: number;
  sumofTrueFalseValues: number;
  selectedIndex = 0;
  isModelSaved = false;

  selectedModelROCAUCvalue: number;
  cpuUsageValue: number;
  memoryUsageInMB: number;
  totalEstimatedRuntime = 0;
  disableRegression: boolean;
  disableTimeSeries: boolean;
  disableClustering: boolean;
  frequencyType: string;
  modelCount = [];
  selectedModelCount = 0;

  retrainModels: boolean;
  modalRef: BsModalRef;
  config = {
    backdrop: true,
    class: 'deploymodle-confirmpopup'
  };

  retrainModelMsg: string;
  multiClassConfusionMatrix;

  reportTblHeading = [];
  reportColVal = [];
  reportCollenght = [];
  selectedMatthewsCoefficient: number;
  genericErrorMessage = `Error occurred: Due
    to some backend data process
     the relevant data could not be produced.
      Please try again while we troubleshoot the error`;
  explainableAImodelName: string;
  explainableAIAccuracyValue: string;
  explainableAIRunTime: string;
  explainableAIROCAUCValue: number;
  explainableAIr2Value: number;
  isInstaML = false;
  screenWidth: any;

  retrainingBtnID: string;
  deliveryTypeName;
  isTextPreProcessing: boolean;
  showTextPreProcessDiv: boolean;

  modelTrainingFlag;
  paramData;
  clientUId;
  deliveryConstructUID;
  readOnly;
  subscription: Subscription;
  trainingCompletionTime;
  barChartWidth : number = 0;
  minimumBarWidth : number = 750;
  decimalPoint : number;

  @Output() isNextDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();

  constructor(private _recommendedAiService: AdRecommendedService, private _dialogService: DialogService,
    private _localStorageService: LocalStorageService, private _notificationService: NotificationService,
    private _router: Router, @Inject(ElementRef) private eleRef: ElementRef,
    private _problemStatementService: ProblemStatementService,
    private domSanitizer: DomSanitizer, private _cookieService: CookieService, private _appUtilsService: AppUtilsService,
    private coreUtilsService: CoreUtilsService,
    private fs: FeatureSelectionService,
    private _modalService: BsModalService, private uts: UsageTrackingService) {
      this.decimalPoint = (sessionStorage.getItem('decimalPoint') != null && sessionStorage.getItem('decimalPoint') != 'null') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
     }

  ngOnInit() {
    this.isNextDisabled.emit(true);
    this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {

      this.paramData = paramData;
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.fs.setApplyFlag(true);
    this.correlationId = this._localStorageService.getCorrelationId(); // '4cd422ae-e5f2-43f2-ada7-76855999fbd2'//
    this.screenWidth = window.innerWidth;
    this.disableRegression = true;
    this.disableTimeSeries = true;
    this.uts.usageTracking('Model Engineering', 'Recommended AI');
    this.getRecommendedAIDetails();
    this.userIdForTrainedModels = this._appUtilsService.getCookies().UserId;
  }

  ngOnChanges() {
    this.screenWidth = window.innerWidth;
  }

  ngAfterViewInit(): void {
    this.setClassOnElement(this.retrainingBtnID, 'btn btn-secondary btncolor');
  }

  getRecommendedAIDetails(): any {
    this._appUtilsService.loadingStarted();
    this.recommendedAiSubscription = this._recommendedAiService
      .getRecommendedAIAD(this.correlationId)
      .pipe(
        catchError(
          // tslint:disable: no-shadowed-variable
          error => {
            this._appUtilsService.loadingImmediateEnded();
            this._notificationService.error(error);
            return throwError(error);
          }))
      .subscribe(data => {


        this.setAttributes(data);
        this.verifyChartData(data);
        this._appUtilsService.loadingImmediateEnded();
      });
  }

  unsubscribe() {
    this.timerSubscripton ? this.timerSubscripton.unsubscribe() : '';
    this.recommendedAiSubscription ? this.recommendedAiSubscription.unsubscribe() : '';
  }

  setAttributes(resAI) {
    if (resAI.CurrentProgress !== undefined) {
      this.pythonProgressVal = resAI.CurrentProgress;
    }
    // this.pythonProgressVal = resAI.CurrentProgress;
    if (!this.coreUtilsService.isNil(resAI.ModelName)) {
      this.pstModelName = resAI.ModelName;
    } if (!this.coreUtilsService.isNil(resAI.DataSource)) {
      this.pstDataSourceName = resAI.DataSource;
      sessionStorage.setItem('DataSource', this.pstDataSourceName);
    } if (!this.coreUtilsService.isNil(resAI.BusinessProblems)) {
      this.pstUseCase = resAI.BusinessProblems;
      sessionStorage.setItem('BusinessProblems', this.pstUseCase);
    }
    if (!this.coreUtilsService.isNil(resAI.Category)) {
      this.deliveryTypeName = resAI.Category;
    }
    this.cpuUsageInTrainning(resAI);
    this.memoryUsageInTrainning(resAI);

    this._cookieService.set('ModelTypeForInstaML', resAI.ModelType);
    this.modelTrainingFlag = resAI.IsModelTrained;
    this.fs.setApplyFlag(!this.modelTrainingFlag);
    this.isRetrainButton = resAI.Retrain;
    this.disableTabs();
    if (!resAI.IsModelTrained) {

      sessionStorage.setItem('isModelDeployed', 'false')

      this.isTrainingStart = resAI.IsModelTrained;
      this.isNextDisabled.emit(!this.isTrainingStart);

      if (resAI.SelectedModels) {
        this._cookieService.delete('ProblemType');
        this._cookieService.set('ProblemType', resAI.SelectedModels.ProblemType);
        this.isRetrainButton = resAI.Retrain;
        if (this.isTrainingStart === false && this.isRetrainButton === true) {
          this.retrainModels = false;
        }

        if (resAI.SelectedModels.ProblemType === 'TimeSeries') {
          this.timeSeriesAIShow = true;
          this.selectedModelOnLoad = resAI.SelectedModels.ProblemType;
          this.loadTimeSeries();
          this.availableModels = resAI.SelectedModels.SelectedModels;
          const newAvailableColumns = {};
          for (const i in this.availableModels) {
            if (this.availableModels[i].Train_model === 'True') {
              newAvailableColumns[i] = this.availableModels[i];
              this.modelCount.push(i);
            }
          }
          for (const j in this.availableModels) {
            if (this.availableModels[j].Train_model === 'False') {
              newAvailableColumns[j] = this.availableModels[j];
            }
          }
          this.availableModels = newAvailableColumns;

          this.findTotalEstimatedRunTIme();

          this.recommendedModelName = Object.keys(this.availableModels);
        } else {
          if (resAI.SelectedModels.ProblemType === 'Regression') {
            this.regressionAIShow = true;
            this.selectedModelOnLoad = resAI.SelectedModels.ProblemType;
            this.loadRegressionModel();
          }
          this.availableModels = resAI.SelectedModels.SelectedModels;
          const newAvailableColumns = {};
          for (const i in this.availableModels) {
            if (this.availableModels[i].Train_model === 'True') {
              newAvailableColumns[i] = this.availableModels[i];
              this.modelCount.push(i);
            }
          }
          for (const j in this.availableModels) {
            if (this.availableModels[j].Train_model === 'False') {
              newAvailableColumns[j] = this.availableModels[j];
            }
          }
          this.availableModels = newAvailableColumns;

          this.findTotalEstimatedRunTIme();

          this.recommendedModelName = Object.keys(this.availableModels);

          this.prescriptionStatus = true;
        }
      }
    } else {
      this.trainedModels = [];
      if (resAI.TrainedModel) {
        this.isNextDisabled.emit(!resAI.TrainedModel);

        this.isInstaML = resAI.InstaFlag;
        this.isTrainingStart = resAI.IsModelTrained;
        this.isRetrainButton = resAI.Retrain;
        this.trainingCompletionTime = Math.round(resAI.EstimatedRunTime * 100) / 100;
        if (this.isTrainingStart === false && this.isRetrainButton === true) {
          this.retrainModels = false;
          // sessionStorage.setItem('isModelDeployed', 'false')
          sessionStorage.setItem('isNewModel', 'true');
          sessionStorage.setItem('isModelTrained', resAI.IsModelTrained);
          this.coreUtilsService.disableTabs(2, 1);
        } else if (this.isTrainingStart === true && this.isRetrainButton === true) {
          this.retrainModels = true;
          // sessionStorage.setItem('isModelDeployed', 'true')
          sessionStorage.setItem('isNewModel', 'false');
          sessionStorage.setItem('isModelTrained', resAI.IsModelTrained);
          this.coreUtilsService.disableTabs(2, 1)
          this.setClassOnElement(this.retrainingBtnID, 'btn btn-primary');
        } else if (this.isTrainingStart === true && this.isRetrainButton === false) {
          this.retrainModels = false;
          // sessionStorage.setItem('isModelDeployed', 'true')
          sessionStorage.setItem('isNewModel', 'false');
          sessionStorage.setItem('isModelTrained', resAI.IsModelTrained);
          this.coreUtilsService.disableTabs(2, 1)
          this.setClassOnElement(this.retrainingBtnID, 'btn btn-primary');
        }


        this._cookieService.delete('ProblemType');
        this._cookieService.set('ProblemType', resAI.SelectedModel);
        if (resAI.SelectedModel === 'regression' || resAI.SelectedModel === 'Regression') {
          this.regressionAIShow = true;
          this.selectedModelOnLoad = resAI.SelectedModel;
          this.loadRegressionModel();
        } else if (resAI.SelectedModel === 'timeseries' || resAI.SelectedModel === 'TimeSeries') {
          this.timeSeriesAIShow = true;
          this.selectedModelOnLoad = resAI.SelectedModel;
          this.loadTimeSeries();
        }


        this.recommendedtrainedModels = resAI.TrainedModel;


        for (let i = 0; i < this.recommendedtrainedModels.length; i++) {
          if (this.selectedModelOnLoad === 'regression' || this.selectedModelOnLoad === 'Regression') {
            this.selectedModelNameforCharts = this.selectedModelOnLoad;

            const trainedModel = new TrainedModels;
            trainedModel.Accuracy = this.recommendedtrainedModels[i]?.r2ScoreVal?.error_rate;
            trainedModel.modelName = this.recommendedtrainedModels[i].modelName;
            trainedModel.RunTime = Math.round(this.recommendedtrainedModels[i].RunTime * 100) / 100;
            this.trainedModels.push(trainedModel);
            this.loadRegressionModel();

          } else if (this.selectedModelOnLoad === 'timeseries' || this.selectedModelOnLoad === 'TimeSeries') {

            this.selectedModelNameforCharts = this.selectedModelOnLoad;
            const trainedModel = new TrainedModels;
            trainedModel.Accuracy = this.recommendedtrainedModels[i]?.r2ScoreVal?.error_rate;
            trainedModel.modelName = this.recommendedtrainedModels[i].modelName;
            trainedModel.RunTime = Math.round(this.recommendedtrainedModels[i].RunTime * 100) / 100;
            this.loadTimeSeries();
            const lineChartData = {};
            lineChartData['Actual'] = this.recommendedtrainedModels[i].Actual;
            lineChartData['Forecast'] = this.recommendedtrainedModels[i].Forecast;
            lineChartData['RangeTime'] = this.recommendedtrainedModels[i].RangeTime;
            lineChartData['Accuracy'] = trainedModel.Accuracy;
            lineChartData['Frequency'] = this.recommendedtrainedModels[i].Frequency;
            lineChartData['ModelName'] = this.recommendedtrainedModels[i].modelName;
            this.lineChart[i] = lineChartData;
            this.trainedModels.push(trainedModel);

          }
        }
        if (this.selectedModelOnLoad === 'timeseries' || this.selectedModelOnLoad === 'TimeSeries') {
          this.setDefaultTimeSeriesModelSelected();
        } else {
          this.setDefaultModelSelected();
        }

        if (this.pythonProgress === 100) {
          this.prescriptionStatus = true;
        } else if (this.pythonProgress === 0 || this.pythonProgress === undefined) {
          this.prescriptionStatus = true;
        } else {
          this.prescriptionStatus = false;
        }
      }
    }
    this.setPrescriptionHeading();

    if (resAI.UseCaseList) {
      if (resAI.TrainedModel === null) {
        if (resAI.CurrentProgress === 0.0) {
          return this._notificationService.error(this.genericErrorMessage);
        } else if (resAI.CurrentProgress <= 98) {
          this.pythonProgressVal = resAI.CurrentProgress;
          this.cpuUsageInTrainning(resAI);
          this.memoryUsageInTrainning(resAI);

          this.retry();

          this.pythonProgressVal = resAI.CurrentProgress;
          this.cpuUsageInTrainning(resAI);
          this.memoryUsageInTrainning(resAI);
        } else if (resAI.CurrentProgress >= 99) {
          this.pythonProgressVal = resAI.CurrentProgress;

          this.cpuUsageInTrainning(resAI);
          this.memoryUsageInTrainning(resAI);
          this.retry();
        } else if (resAI.CurrentProgress === 100) {
          this.pythonProgressVal = resAI.CurrentProgress;
          this._recommendedAiService.getRecommendedTrainedModelsAD(this.correlationId, this.userIdForTrainedModels,
            this.pageInfoForTrainedModels, this.selectedModelCount).subscribe(
              data => {
                this.setAttributes(data);
                this.verifyChartData(data);
              });
          this.unsubscribe();
        }
      }
    }
  }

  findTotalEstimatedRunTIme() {
    const runtimeSelectedModel = {};
    for (const i in this.availableModels) {
      if (Object.keys(this.availableModels).length !== 0) {
        this.recommendedModelNamesBeforeTraining.push(i);
        this.runTimeOfRecommendedModels[i] = this.availableModels[i].EstimatedRunTime;
        this.isModelChecked[i] = this.availableModels[i].Train_model;

        runtimeSelectedModel[i] = this.runTimeOfRecommendedModels[i];

        if (this.isModelChecked[i] === 'False') {
          runtimeSelectedModel[i] = 0;
        }
        this.totalEstimatedRuntime = Math.round((this.totalEstimatedRuntime + runtimeSelectedModel[i]) * 100) / 100;
      }
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
      this.startModelTraining();
    });
    return this.timerSubscripton;
  }

  startTrainingAI() {
    this.isModelTrained = false;
    this.uts.usageTracking('Create Anomaly Model', 'Models Trained');

    if (this.modelCount.length > 0) {
      this.selectedModelCount = this.modelCount.length;
    } else {
      this.selectedModelCount = this.recommendedModelNamesBeforeTraining.length;
    }

    // if (!this.isModelSaved) {
    //   this.postStartTrainningData['CorrelationId'] = this.correlationId;
    //   this._recommendedAiService.saveModelPrefrences(this.postStartTrainningData).subscribe(rsponse => {
    //     if (rsponse === 'Success') {
    //       this.isModelSaved = true;
    //       this.startModelTraining();
    //     } else {
    //       this._notificationService.error('Something went wrong to saveModelPrefrences.');
    //     }
    //   }, error => {
    //     this._notificationService.error('Something went wrong to saveModelPrefrences.');
    //   });
    // } else {
    this.startModelTraining();
    // }
  }

  startModelTraining() {
    this._recommendedAiService.getRecommendedTrainedModelsAD(this.correlationId, this.userIdForTrainedModels,
      this.pageInfoForTrainedModels, this.selectedModelCount)
      .pipe(
        switchMap(
          data => {

            if (data !== '' || data !== null) {
              if (this.IsJsonString(data)) {
                const parsedData = JSON.parse(data);
                if (parsedData.hasOwnProperty('message')) {
                  if (parsedData.message === 'Success..' || parsedData.message === 'Request is in Progress') {
                    return this._recommendedAiService.getRecommendedTrainedModelsAD(this.correlationId, this.userIdForTrainedModels,
                      this.pageInfoForTrainedModels, this.selectedModelCount);
                  }
                } else if (parsedData.hasOwnProperty('UseCaseList')) {
                  return of(parsedData);
                } else {
                  this.noData = 'Format from server is not Recognised';
                  this._notificationService.error(this.genericErrorMessage);
                  this.unsubscribe();
                  // tslint:disable-next-line: deprecation
                  return empty();
                }
              } else if (data.constructor === Object) {
                this.setAttributes(data);
                this.verifyChartData(data);
                // tslint:disable-next-line: deprecation
                return empty();
              } else if (data.constructor === String) {
                if (data === '') {
                  this._notificationService.error(this.genericErrorMessage);
                } else if (data === 'bc') {
                  this._notificationService.error(this.genericErrorMessage);
                } else {
                  this.noData = data?.toString();
                  this._notificationService.success(data);
                }
                this.unsubscribe();
                // tslint:disable-next-line: deprecation
                return empty();
              } else {
                this.noData = 'Format from server is not Recognised';
                this._notificationService.error(this.genericErrorMessage);
                this.unsubscribe();
              }
            } else {
              this._notificationService.error(this.genericErrorMessage);
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
            if (responseData.message === 'Request is in Progress') {
              this.retry();
            }
          }
        } else if (data.constructor === Object) {
          this.setAttributes(data);
          this.verifyChartData(data);
        }
      });
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

  onSwitchChanged(elementRef, modelName) {
    if (elementRef.checked === true) {
      this.postStartTrainningData.SelectedModels[modelName] = {
        Train_model: 'True'
      };
    }
    if (elementRef.checked === false) {
      this.postStartTrainningData.SelectedModels[modelName] = {
        Train_model: 'False'
      };
    }

    let switchSelection = {};
    const eachModelTime = {};
    switchSelection = this.postStartTrainningData['SelectedModels'];

    this.totalEstimatedRuntime = 0;
    this.modelCount = [];
    for (const i in this.availableModels) {
      if (Object.keys(this.availableModels).length !== 0) {
        for (const p in switchSelection) {
          if (i === p) {
            this.availableModels[i].Train_model = switchSelection[p].Train_model;
          }
        }
        eachModelTime[i] = this.availableModels[i].EstimatedRunTime;
        if (this.availableModels[i].Train_model === 'False') {
          eachModelTime[i] = 0;
          this.totalEstimatedRuntime = Math.round((this.totalEstimatedRuntime + eachModelTime[i]) * 100) / 100;
        } else if (this.availableModels[i].Train_model === 'True') {
          this.totalEstimatedRuntime = Math.round((this.totalEstimatedRuntime + this.availableModels[i].EstimatedRunTime) * 100) / 100;
        }

        for (const p in switchSelection) {
          if (i === p) {
            if (this.availableModels[i].Train_model === 'False' && switchSelection[p].Train_model === 'True') {
              this.totalEstimatedRuntime = Math.round((this.totalEstimatedRuntime + this.availableModels[i].EstimatedRunTime) * 100) / 100;
            }
          }
        }
        if (this.availableModels[i].Train_model === 'True') {
          this.modelCount.push(i);
        }
      }
    }
    if (this.modelCount.length === 0) {
      this.isModelTrained = false;
    } else {
      this.isModelTrained = true;
    }
  }


  saveSelectedModels() {
    if (!this.readOnly) {
      this.postStartTrainningData['CorrelationId'] = this.correlationId;
      this._recommendedAiService.saveModelPrefrences(this.postStartTrainningData).subscribe(rsponse => {
        if (rsponse === 'Success') {
          if (!this.isModelSaved) {

          } else {
            this._notificationService.success('Data Saved Successfully');
          }
          this.isModelSaved = true;
        } else {
          this._notificationService.error('Something went wrong to saveModelPrefrences.');
        }
      }, error => {
        this._notificationService.error('Something went wrong to saveModelPrefrences.');
      });
    }
  }

  saveAs($event) {
    const openTemplateAfterClosed = this._dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this._problemStatementService.clone(this.correlationId, modelName);
        } else {
          // tslint:disable-next-line: deprecation
          return empty();
        }
      }),

      tap(data => this.newCorrelationIdAfterCloned = data),

      tap(data => {
        if (data) {
          this._router.navigate(['anomaly-detection/modelengineering/RecommendedAI'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });
          this._notificationService.success('Cloned Successfully');
        }
      })
    );
    openTemplateAfterClosed.subscribe();
  }

  reStartTraining(confirmRetrainModels: TemplateRef<any>, $event) {
    this._recommendedAiService.ValidateInput(this.correlationId, 'RestartTrain').subscribe(data => {
      if (data['Status'] === 'C') {
        localStorage.removeItem('SelectedRecommendedModel');
        this.retrainModelMsg = 'Do you want to retrain the model? Web Service will be impacted. Click Yes if you wish to Proceed or No to Return';
        this.modalRef = this._modalService.show(confirmRetrainModels, this.config);
      } else {
        this._notificationService.error(data['Message']);
      }
    });
  }

  // confirmation popup ok event
  confirm() {
    this._appUtilsService.loadingStarted();
    this._recommendedAiService.retrainTrainedModel(this.correlationId, this.userIdForTrainedModels).subscribe(retrainRec => {
      if (retrainRec != null) {
        if (retrainRec === 'Success') {
          this.pythonProgressVal = 0;
          this.getRecommendedAIDetails();
          this.retrainModels = false;
          this.setClassOnElement(this.retrainingBtnID, 'btn btn-secondary btncolor');
        }
        this._appUtilsService.loadingImmediateEnded();
      }
    }, error => {
      this._appUtilsService.loadingImmediateEnded();
      this._notificationService.error('Something went wrong to StartRetrainModels.');
    });

    this.modalRef.hide();

  }

  // confirmation popup cancle event
  decline() {
    this.modalRef.hide();
  }


  getBestModel(accuracyValue, modelName, runtime, index: number) {
    this.selectedIndex = index;
    this.screenWidth = window.innerWidth;
    this.selectedAccuracyValue = accuracyValue;
    this.selectedTrainedModelName = modelName;
    this.selectedModelRunTimeValue = (Math.round(runtime * 100) / 100)?.toString();
    this.receiveSelectAccuracy(accuracyValue);

  }
  receiveSelectAccuracy($event, trainedModel?) {
    if ($event !== undefined) {
      this._cookieService.delete('');
      localStorage.setItem('SelectedModelAccuracy', this.selectedAccuracyValue?.toString());
      this.isRegressionR2Val = false;
      this.isTimeSeriesVal = false;
      this.showTextPreProcessDiv = false;
      if (this.selectedModelOnLoad === 'regression' || this.selectedModelOnLoad === 'Regression') {
        this.isRegressionR2Val = true;
        this.getRegOrClassModelOnSelection(this.selectedAccuracyValue);
      } else if (this.selectedModelOnLoad === 'timeseries' || this.selectedModelOnLoad === 'TimeSeries') {
        this.isTimeSeriesVal = true;
        this.getTimeseriesLineChart(this.selectedAccuracyValue, trainedModel);
      }
    }
  }

  getRegOrClassModelOnSelection(selectedAccuracyValue) {

    if (this.selectedModelOnLoad !== 'Text_Classification') {
      this.getFeatureWeight(selectedAccuracyValue);
    }
    this.getSelectedModelName(selectedAccuracyValue);
    this.getRunTime(selectedAccuracyValue);
    this.getR2SValue(selectedAccuracyValue);
    this.generateArrayForConfusionMatrix(selectedAccuracyValue);

    this.getFineTuneBase64Image(selectedAccuracyValue);
    this.getF1Score(selectedAccuracyValue);

  }

  getFeatureWeight(selectedAccuracy) {
    const accuracy = [];
    const ClusteringFlag = [];

    for (const t in this.recommendedtrainedModels) {
      if (this.recommendedtrainedModels[t].Accuracy) {
        accuracy.push(Math.round(this.recommendedtrainedModels[t].Accuracy * 100) / 100);
        ClusteringFlag.push(this.recommendedtrainedModels[t].Clustering_Flag);
      } else if (this.recommendedtrainedModels[0]?.r2ScoreVal?.error_rate) {
        accuracy.push(this.recommendedtrainedModels[t]?.r2ScoreVal?.error_rate);
        ClusteringFlag.push(this.recommendedtrainedModels[t].Clustering_Flag);
      } else if (this.recommendedtrainedModels[0]?.r2ScoreVal?.error_rate === 0) {
        accuracy.push(this.recommendedtrainedModels[t]?.r2ScoreVal?.error_rate);
        ClusteringFlag.push(this.recommendedtrainedModels[t].Clustering_Flag);
      }
    }

    // featureWeight = Object.keys(this.featureWeight).map(item => this.featureWeight[item]);

    for (let a = 0; a < accuracy.length; a++) {
      for (let fc = a; fc < ClusteringFlag.length; fc++) {
        if (selectedAccuracy === accuracy[fc]) {
          this.isClusteringFlag = ClusteringFlag[fc];
        }
      }
    }
  }

  getF1Score(selectedAccuracy) {
    if (this.trainedModels[this.selectedIndex]['f1Score'] !== undefined) {
      const f1ScoreValue = this.trainedModels[this.selectedIndex]['f1Score'];
      this.selectedModelF1Score = Math.round(f1ScoreValue * 100) / 100;
    }
  }

  getSelectedModelName(selectedAccuracy) {
    if (this.selectedModelRunTimeValue === undefined) {
      const model = this.trainedModels.filter(model => model.Accuracy === selectedAccuracy);
      this.selectedTrainedModelName = model[0]["modelName"];
    }

    localStorage.removeItem('SelectedRecommendedModel');
    localStorage.setItem('SelectedRecommendedModel', this.selectedTrainedModelName.trim());
  }

  getRunTime(selectedAccuracy) {
    if (this.selectedModelRunTimeValue === undefined) {
      const model = this.trainedModels.filter(model => model.Accuracy === selectedAccuracy);
      this.selectedModelRunTimeValue = model[0]['RunTime'];
    }
    localStorage.removeItem('SelectedModelRunTime');
    localStorage.setItem('SelectedModelRunTime', this.selectedModelRunTimeValue?.toString());
  }

  getFineTuneBase64Image(selectedAccuracy) {
    if (this.trainedModels[this.selectedIndex]['img_64'] !== undefined) {
      this.selectedFineTuneImg_64 = this.trainedModels[this.selectedIndex]['img_64'].substring(2).slice(0, -1);
      this.selectedFineTuneImg_64 = 'data:image/png;base64,' + this.selectedFineTuneImg_64;
    }
  }

  getR2SValue(selectedAccuracy) {
    this.selectedModelr2Values = selectedAccuracy;
  }

  setDefaultVal() {
    this.setClassOnElement('btnRegression', 'anchor-disabled');
    this.setClassOnElement('btnTimeSeries', 'anchor-disabled');
    this.setClassOnElement('btnClustering', 'anchor-disabled');
    this.disableRegression = true;
    this.disableTimeSeries = true;
  }

  loadRegressionModel() {
    this.setDefaultVal();
    this.disableRegression = false;
    this.setClassOnElement('btnRegression', 'ingrain-reg-item text-decoration-none active');
  }

  loadTimeSeries() {
    this.setDefaultVal();
    this.disableTimeSeries = false;
    this.setClassOnElement('btnTimeSeries', 'ingrain-reg-item text-decoration-none active');
  }

  setClassOnElement(elementId: string, className: string) {
    //   this.eleRef.nativeElement.parentElement.querySelector('#' + elementId).className = className;
  }

  prescriptionOpen() {
    this.prescriptionStatus = !this.prescriptionStatus;
    if (this.isInstaML) {
      if (this.trainedModels.length > 2) {
        if (this.prescriptionStatus === true) {
          this.setClassOnElement('carouselPrevious', 'carousel-control-prev hide-carousel-prev');
        } else if (this.prescriptionStatus === false) {
          this.setClassOnElement('carouselPrevious', 'carousel-control-prev');
        }
      }
    } else {
      if (this.prescriptionStatus && this.isTrainingStart) {
        this.setClassOnElement('carouselPrevious', 'carousel-control-prev hide-carousel-prev');
      } else if (this.prescriptionStatus === false && this.isTrainingStart) {
        this.setClassOnElement('carouselPrevious', 'carousel-control-prev');
      }
    }
  }

  setPrescriptionHeading() {
    if (this.isTrainingStart) {
      this.prescriptionTemplateText = 'EXPLAINABLE AI';
      this.setClassOnElement('dvDescriptionText', 'ingrAI-explainableAI-text');
    } else {
      this.prescriptionTemplateText = 'Prescription';
      this.setClassOnElement('dvDescriptionText', 'ingerAI-predescription-text');
    }
  }

  setDefaultTimeSeriesModelSelected() {
    const accuracy = [];
    for (const t in this.trainedModels) {
      if (this.trainedModels.length > 0) {
        accuracy.push(this.trainedModels[t].Accuracy);
      }
    }
    this.defaultModelAccuracyOnLoad = accuracy.reduce((a, b) => Math.max(a, b));
    localStorage.setItem('SelectedModelAccuracy', this.defaultModelAccuracyOnLoad?.toString());

    for (let a = 0; a <= accuracy.length; a++) {
      if (this.defaultModelAccuracyOnLoad === accuracy[a]) {
        this.selectedIndex = a;
      }
    }

    const modelWithMaximumAccuracy = this.trainedModels.splice(this.selectedIndex, 1);
    this.trainedModels = modelWithMaximumAccuracy.concat(this.trainedModels);
    this.selectedIndex = 0;
    this.bestFitModel(this.defaultModelAccuracyOnLoad);
    this.isRegressionR2Val = false;
    this.isTimeSeriesVal = true;
    this.showTextPreProcessDiv = false;

    this.selectedAccuracyValue = this.defaultModelAccuracyOnLoad;

  }


  setDefaultModelSelected() {
    const accuracy = [];
    localStorage.removeItem('SelectedModelAccuracy');
    for (const t in this.trainedModels) {
      if (this.trainedModels.length > 0) {
        accuracy.push(this.trainedModels[t].Accuracy);
      }
    }
    this.defaultModelAccuracyOnLoad = accuracy.reduce((a, b) => Math.max(a, b));
    localStorage.setItem('SelectedModelAccuracy', this.defaultModelAccuracyOnLoad?.toString());

    const modelAcc = this.trainedModels.filter(model => model.Accuracy === this.defaultModelAccuracyOnLoad);
    // if (modelAcc.length > 1) {
    // this.selectedIndex = 0;
    // } else {
    for (let a = 0; a < accuracy.length; a++) {
      if (this.defaultModelAccuracyOnLoad === accuracy[a]) {
        this.selectedIndex = a;
      }
    }
    // }

    if (this.selectedModelOnLoad === 'regression' || this.selectedModelOnLoad === 'Regression') {
      this.bestFitModel(this.defaultModelAccuracyOnLoad);
      this.isRegressionR2Val = true;
      this.showTextPreProcessDiv = false;
    }

    const modelWithMaximumAccuracy = this.trainedModels.splice(this.selectedIndex, 1);
    this.trainedModels = modelWithMaximumAccuracy.concat(this.trainedModels);
    this.selectedIndex = 0;

    this.selectedAccuracyValue = this.defaultModelAccuracyOnLoad;

    this.getRegOrClassModelOnSelection(this.defaultModelAccuracyOnLoad);
  }

  generateArrayForConfusionMatrix(selectedAccuracy) {
    const accuracy = [];
    let falseNegativeVal = [];
    let falsePositiveVal = [];
    let trueNegativeVal = [];
    let truePositiveVal = [];

    for (const t in this.recommendedtrainedModels) {
      if (this.recommendedtrainedModels[t].Accuracy) {
        accuracy.push(Math.round(this.recommendedtrainedModels[t].Accuracy * 100) / 100);
      } else if (this.recommendedtrainedModels[0]?.r2ScoreVal?.error_rate) {
        accuracy.push(this.recommendedtrainedModels[t]?.r2ScoreVal?.error_rate);
      }
    }

    falseNegativeVal = Object.keys(this.falseNegative).map(item => this.falseNegative[item]);
    falsePositiveVal = Object.keys(this.falsePositive).map(item => this.falsePositive[item]);
    trueNegativeVal = Object.keys(this.trueNegative).map(item => this.trueNegative[item]);
    truePositiveVal = Object.keys(this.truePositive).map(item => this.truePositive[item]);

    for (let a = 0; a < accuracy.length; a++) {
      for (let f = a; f < falseNegativeVal.length; f++) {
        if (selectedAccuracy === accuracy[f]) {
          this.selectedModelFalseNegative = falseNegativeVal[f];
        }
      }
      for (let f = a; f < falsePositiveVal.length; f++) {
        if (selectedAccuracy === accuracy[f]) {
          this.selectedModelFalsePositive = falsePositiveVal[f];
        }
      }
      for (let f = a; f < trueNegativeVal.length; f++) {
        if (selectedAccuracy === accuracy[f]) {
          this.selectedModelTrueNegative = trueNegativeVal[f];
        }
      }

      for (let f = a; f < truePositiveVal.length; f++) {
        if (selectedAccuracy === accuracy[f]) {
          this.selectedModelTruePositive = truePositiveVal[f];
        }
      }
    }

    this.totalOfTrueFalsePositive = this.selectedModelFalsePositive + this.selectedModelTruePositive;
    this.totalofTrueFalseNegative = this.selectedModelFalseNegative + this.selectedModelTrueNegative;

    this.totalofTruePositiveFalseNegative = this.selectedModelTruePositive + this.selectedModelFalseNegative;
    this.totalofFalsePositiveTrueNegative = this.selectedModelFalsePositive + this.selectedModelTrueNegative;
    this.sumofTrueFalseValues = this.selectedModelFalsePositive + this.selectedModelTruePositive +
      this.selectedModelFalseNegative + this.selectedModelTrueNegative;
  }


  getFormatedConfusionMatirxImg(selectedAccuracy) {
    if (this.trainedModels[this.selectedIndex]['confustionMatirxImg_64'] !== undefined) {
      this.multiClassConfusionMatrix = this.trainedModels[this.selectedIndex]['confustionMatirxImg_64'].substring(2).slice(0, -1);
      this.multiClassConfusionMatrix = this.domSanitizer.bypassSecurityTrustResourceUrl(
        'data:image/png;base64,' + this.multiClassConfusionMatrix);
    }
  }



  getMatthewsCoefficient(selectedAccuracy) {
    if (this.trainedModels[this.selectedIndex]['matthewsCoefficient'] !== undefined) {
      this.selectedMatthewsCoefficient = this.trainedModels[this.selectedIndex]['matthewsCoefficient'];
    }
  }

  onCarouselNextBtn() {
    const spliced = this.trainedModels.splice(0, 4);
    this.trainedModels = this.trainedModels.concat(spliced);
    this.selectedAccuracyValue = this.trainedModels[this.selectedIndex]['Accuracy'];
    this.selectedTrainedModelName = this.trainedModels[this.selectedIndex]['modelName'];
    this.selectedModelRunTimeValue = (Math.round(this.trainedModels[this.selectedIndex]['RunTime'] * 100) / 100)?.toString();
    this.receiveSelectAccuracy(this.selectedAccuracyValue, this.trainedModels[this.selectedIndex]);
  }

  onCarouselPreviousBtn() {
    const length = this.trainedModels.length;
    const spliced = this.trainedModels.splice(length - 4, 4);
    this.trainedModels = spliced.concat(this.trainedModels);
    this.selectedAccuracyValue = this.trainedModels[this.selectedIndex]['Accuracy'];
    this.selectedTrainedModelName = this.trainedModels[this.selectedIndex]['modelName'];
    this.selectedModelRunTimeValue = (Math.round(this.trainedModels[this.selectedIndex]['RunTime'] * 100) / 100)?.toString();
    this.receiveSelectAccuracy(this.selectedAccuracyValue, this.trainedModels[this.selectedIndex]);
  }

  bestFitModel(selectedAccuracy) {

    this.explainableAIROCAUCValue = this.trainedModels[this.selectedIndex]['ROCAUCvalue'];
    this.explainableAIr2Value = this.trainedModels[this.selectedIndex]['Accuracy'];

    const model = this.trainedModels.filter(model => model.Accuracy === selectedAccuracy);
    this.explainableAImodelName = model[0]["modelName"];
    this.explainableAIRunTime = model[0]["RunTime"];
    this.explainableAIAccuracyValue = model[0]['Accuracy'];

    const index = this.trainedModels.findIndex(model => model.modelName === this.explainableAImodelName)
    this.selectedIndex = index;

    this.selectedTrainedModelName = this.explainableAImodelName;
  }

  getRetrainBtnId(btnId) {
    this.retrainingBtnID = btnId;
  }

  private disableTabs() {
    // this.ps.getColumnsdata.IsModelTrained
    const classnameDisabled = 'anchor-disabled';
    const classnameEnabled = 'anchor-enabled';

    const nativeElemnt = !this.coreUtilsService.isNil(this.eleRef.nativeElement) ? this.eleRef.nativeElement : null;
    const parentEle = !this.coreUtilsService.isNil(nativeElemnt) ? nativeElemnt.parentElement : null;
    const nestedParentEle = !this.coreUtilsService.isNil(parentEle) ? parentEle.parentElement : null;
  }

  getTimeseriesLineChart(selectedAccuracyValue, trainedMOdel) {

  }

  verifyChartData(data) {
    if (!this.coreUtilsService.isNil(data.TrainedModel)) {
      if (this.isTimeSeriesVal == true) {
        //this.prepareTimeSeriesChart(data.TrainedModel[0]);
      } else {
        if (!this.coreUtilsService.isNil(data.TrainedModel[0].Anomalyvalues) && !this.coreUtilsService.isNil(data.TrainedModel[0].y_axis)) {
          //if (data.TrainedModel[0].Anomalyvalues.length == data.TrainedModel[0].y_axis.length) {
          if (data.TrainedModel[0].Anomalyvalues.length && data.TrainedModel[0].y_axis.length) {
            this.prepareRegressionChart(data.TrainedModel[0].Anomalyvalues, data.TrainedModel[0].y_axis);
            this.barChartWidth = (this.barChartData.length > 100 ) ? this.barChartData.length * 10 : this.minimumBarWidth;
          } else {
            this._notificationService.error('Invalid Chart Data found');
          }
        }
      }
    }
  }

  prepareRegressionChart(Xaxis, Yaxis) {
    Xaxis = (typeof (Xaxis) == 'string') ? JSON.parse(Xaxis) : Xaxis;
    for (var i = 0; i < Xaxis.length; i++) {
      this.barChartData.push({ Xaxis: Xaxis[i], Yaxis: Yaxis[i], key: ' '.repeat(i) });
    }
  }

  prepareTimeSeriesChart(trainedModel) {
    for(var i=0;i< trainedModel.Actual.length;i++){
      this.lineChartData.push({ date: trainedModel.RangeTime[i].replace(' 00:00:00',''), 
        Actual : trainedModel.Actual[i],
        Forecast : trainedModel.Forecast[i]});
    }
  }

  getDecimalValue(value){
    if(!this.coreUtilsService.isNil(value)){
      return parseFloat(value).toFixed(this.decimalPoint);
    }
  }

}
