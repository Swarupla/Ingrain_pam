import { Component, ElementRef, EventEmitter, Inject, OnDestroy, OnInit, Output, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { empty, Subscription, timer } from 'rxjs';
import { tap, switchMap } from 'rxjs/operators';
import { DialogService } from 'src/app/dialog/dialog.service';
import { ApiStatus } from 'src/app/_enums/api-status';
import { AdProblemStatementService } from 'src/app/_services/ad-problem-statement.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CascadeModelsService } from 'src/app/_services/cascade-models.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { StoreService } from 'src/app/_services/store.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ShowDataComponent } from '../../dashboard/data-engineering/preprocess-data/show-data/show-data.component';
import { DatasourceChangeComponent } from '../../dashboard/problem-statement/datasource-change-popup/datasource-change.component';
import { PstFeatureMappingComponent } from '../../dashboard/problem-statement/pst-feature-mapping-popup/pst-feature-mapping.component';
import { RegressionPopupComponent } from '../../dashboard/problem-statement/regression-popup/regression-popup.component';
import { ValidRecordDetailsPopupComponent } from '../../dashboard/problem-statement/valid-record-details-popup/valid-record-details-popup.component';
import { FilesMappingModalComponent } from '../../files-mapping-modal/files-mapping-modal.component';
import { TemplateNameModalComponent } from '../../template-name-modal/template-name-modal.component';
import { UploadProgressComponent } from '../upload-progress/upload-progress.component';
import { ChangeDataSourceComponent } from './change-data-source/change-data-source.component';

@Component({
  selector: 'app-ad-usecase-definition',
  templateUrl: './ad-usecase-definition.component.html',
  styleUrls: ['./ad-usecase-definition.component.scss']
})
export class AdUsecaseDefinitionComponent implements OnInit, OnDestroy {

    UniquenessDetails: any;
    validRecordDetails;
    problemStatement = '';
    searchText: string;
    selectedTemplate;
    displayUploadandDataSourceBlock: Boolean = false;
    modelName;
    pageSource;
    correlationId;
    inputColumns: Set<string> = new Set();
    targetColumn = '';
    oldTargetColumn = '';
    columnData: Set<string>;
    currentUser;
    files = [];
    Myfiles = [];
    templateName: string;
    featuresColumn: [];
    isExistingTemplate: boolean;
    isNewModel: boolean;
    isIE: boolean;
    clonedModelName: string;
    newCorrelationIdAfterCloned: string;
    newModelName: string;
    displayDataSourceLink: boolean;
    showSaveAs = false;
    dataSourceName: string;
    showSave = true;
    columnMappingCorrelationId;
    targetColButtonDisabled = false;
    uniqueIdentifierDisabled = false;
    timeSeriesColumnsDisabled = false;
    showSelectAll = false;
    hoursList = ['1', '3', '6', '9', '10', '12', '15', '18', '21', '24'];
    yearlyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    dailyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9',
      '10', '11', '12', '13', '14', '15', '16', '17', '18', '19',
      '20', '21', '22', '23', '24', '25', '26', '27', '28', '29', '30'];
    weeklyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    monthlyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    quarterlyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    halfYearlyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    fortnightlyList = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10'];
    customDaysList = ['1', '2', '3', '4', '5', '6', '7', '8', '9',
      '10', '11', '12', '13', '14', '15', '16', '17', '18', '19',
      '20', '21', '22', '23', '24', '25', '26', '27', '28', '29', '30'];
    targetAggregation = ['None', 'Sum', 'Product', 'Mean', 'Median', 'LabelCounts', 'Count', 'Max', 'Min'];
    isTimeSeriesOn = false;
    timeSeries: FormGroup;
    validationForDropDownVal = false;
    targetAggregationVal = false;
    targetAggVal = '';
    uniqueIdentifierVal = '';
    oldUniqueIdentifierVal = '';
    timeSeriesColumns = '';
    timeSeriesCheckboxValues;
    timeSeriesCheckboxValues1 = {
      '0': {
        'Name': 'Hourly',
        'Steps': null,
      },
      '1': {
        'Name': 'Daily',
        'Steps': null
      },
      '2': {
        'Name': 'Weekly',
        'Steps': null
      },
      '3': {
        'Name': 'Monthly',
        'Steps': null
      },
      '4': {
        'Name': 'Fortnightly',
        'Steps': null
      },
      '5': {
        'Name': 'Quarterly',
        'Steps': null
      },
      '6': {
        'Name': 'Half-Year',
        'Steps': null
      },
      '7': {
        'Name': 'Yearly',
        'Steps': null
      },
      '8': {
        'Name': 'CustomDays',
        'Steps': null,
        'value': null
      }
    };
    timeSeriesObject = {};
    columnDataTypes;
    problemType = 'File';
    labelPosition = 'before';
    color = 'accent';
    targetAggDisable = true;
    paramData: any;
    subscription: any;
    hideTargetValForNone: boolean;
    onload: number;
    instaMLModel = false;
    inputColumnsDisabled = false;
    currentModelName: any;
    metricData;
    filteredMetricData;
    deliveryTypeName;
    entityData;
    tableData = [];
    showDataColumnsList = [];
    problemTypeFlag = false;
    userid: string;
    customTargetColumn: Set<string> = new Set();
    showCustomTarget = false;
    env;
    requestType;
    fromApp;
    readOnly;
    isMappedTimeSeriesModel = false;
    timeSeriesColPostMapping;
    marketPlaceRedirectedUser = false;
    categoryType;
    toggleSelect = true;
    isCustomAttrClicked = false;
    custTargetName = '';
    isDirty = 'No';
    currentIndex = 0;
    breadcrumbIndex = 0;
  
    prebuiltInputAction = false;
    customModelNames = [];
    modalRef: BsModalRef | null;
    config = {
      backdrop: true,
      ignoreBackdropClick: true,
      class: 'deploymodle-confirmpopup'
    };
    timerSubscripton: Subscription;
    latestTargetValue : string = '';
    isModelTrained : boolean = false;
  
  
    @Output() isNextDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();
  
    @Output() currentModel = new EventEmitter();
  
    isModelSaved = false;
    mappingData;
    mapData;
    customCascadeDataForSave;
    cascadeModelName;
    customCascadedId = null;
    newConfig = {
      backdrop: true,
      ignoreBackdropClick: true,
      class: 'deploymodle-confirmpopup',
      source: '',
      target: '',
      cascadedId: '',
      cascadeModelName: '',
      AppId: ''
    };
    dataForCascadeGraph;
    allAttributes = [];
    svgHeight = 600;
    svgWidth = 1200;
    modelNames = [];
    targetIdArray = [undefined, undefined, undefined, undefined];
    uniqueIdArray = [[undefined, undefined], [undefined, undefined], [undefined, undefined], [undefined, undefined]];
    isExpandTab = {
      'hourly': true,
      'daily': false,
      'weekly': false,
      'monthly': false,
      'fortnightly': false,
      'quarterly': false,
      'halfYearly': false,
      'yearly': false,
      'customDays': false
    }
    objectKeys = Object.keys;
    cascadeModelCount = 0;
    isIncludedinCustomCascade = false;
    customCascadeSource;
    FMAppId = 'd1c0dd24-21c3-47f1-8831-302fb0c95d82';
    FMscenario = false;
  
    previousModelTarget = '';
    proba1Value = '';
    disableCascadeSave = false;
    FMSubscription: Subscription;
    completedModels = [];
    @ViewChild('FMScenarioSuccessPopup', { read: TemplateRef, static: true }) FMScenarioPopup: TemplateRef<any>;
    @ViewChild('FMScenarioTrainingStatus', { read: TemplateRef, static: true }) FMScenarioTrainingStatus: TemplateRef<any>;
    isFMModel = false;
  
    dataFrequency;
    iFModelDeployed;
    iFModelTrained;
  
    IfModelTemplateDataSource = false;
    customApiViewEnabled = false;
    customQueryViewEnabled = false;
    previousQuery : string;
    decimalPoint;
  
    constructor(private route: ActivatedRoute, @Inject(ElementRef) private eleRef: ElementRef,
      private coreUtilService: CoreUtilsService,
      private ns: NotificationService, private dialogService: DialogService,
      private appUtilsService: AppUtilsService,
      private router: Router, private ls: LocalStorageService, private formBuilder: FormBuilder,
      private uts: UsageTrackingService, private _storeService: StoreService, private customRouter: CustomRoutingService,
      private cascadeService: CascadeModelsService, private _modalService: BsModalService, private envService: EnvironmentService,
      private adProblemStatementService : AdProblemStatementService) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
       }
  
    ngOnInit() {
      if (sessionStorage.getItem('isModelTrained') === 'true') {
        sessionStorage.removeItem('isModelTrained');
      }
      if (sessionStorage.getItem('isModelDeployed') === 'true') {
        sessionStorage.removeItem('isModelDeployed');
      }
      this.readOnly = sessionStorage.getItem('viewEditAccess');
      this.customCascadedId = sessionStorage.getItem('customCascadedId');
      if (this.readOnly === 'true') {
        this.targetColButtonDisabled = true;
        this.uniqueIdentifierDisabled = true;
        this.timeSeriesColumnsDisabled = true;
        this.inputColumnsDisabled = true;
        this.showSave = false;
      }
      this.isNextDisabled.emit(false);
  
      this.uts.usageTracking('Problem Statement', 'Use case definition');
      // console.log('hi')
      this.env = sessionStorage.getItem('Environment');
      this.requestType = sessionStorage.getItem('RequestType');
      if (sessionStorage.getItem('fromSource') !== null) {
        this.fromApp = sessionStorage.getItem('fromSource').toUpperCase();
      }
      this.hideTargetValForNone = true;
      this.onload = 1;
      // this.timeSeriesCheckboxValues = this.timeSeriesCheckboxValues1;
      this.timeSeries = this.formBuilder.group({
        timeSeriesSelect: '',
        hourlyControl: '',
        dailyControl: '',
        weeklyControl: '',
        biWeeklyControl: '',
        monthlyControl: '',
        fortNightlyControl: '',
        quarterlyControl: '',
        halfYearlyControl: '',
        yearlyControl: '',
        customDaysValueControl: ['', [Validators.min(1), Validators.max(30)]],
        customDaysControl: '',
        targetAggregationControl: ''
      });
  
      // added for marketplace redirected users : start
      if (localStorage.getItem('marketPlaceRedirected') === 'True') {
        this.correlationId = this.ls.getMPCorrelationid();
      } else {
        this.correlationId = this.ls.getCorrelationId();
      }
      // added for marketplace redirected users : end
      this.subscription = this.appUtilsService.getParamData().subscribe(paramData => {
  
        this.paramData = paramData;
        // this.getMetricsData(this.paramData.clientUID);
      });
      this.userid = this.appUtilsService.getCookies().UserId;
      this.isIE = this.coreUtilService.isIE();
      // this.correlationId = this.ls.getCorrelationId();
      // if condition add for marketplace user redirection to selected template
      if ((localStorage.getItem('marketPlaceRedirected')) === 'True') {
        // this.checkUserAccess(this.userid);
        this.displayUploadandDataSourceBlock = true;
        this.selectedTemplate = this.ls.getMPModelCategory();
        this.modelName = this.ls.getMPModelName();
        this.setData();
      } else if (this.correlationId !== undefined) {
        const _this = this;
        /* _this.eleRef.nativeElement.parentElement.parentElement.querySelectorAll('.ingrAI-sub-nav')[0]
           .lastElementChild.querySelectorAll('.btn-primary')[1].className = 'btn btn-primary bt-bg-color'; */
        this.route.queryParams
          .subscribe(params => {
            if (this.router.url.includes('cascadeModels') !== true && this.router.url.includes('WhatIfAnalysis') !== true) {
              this.displayUploadandDataSourceBlock = (params.displayUploadandDataSourceBlock === 'true');
              if (localStorage.getItem('featureMapping') === 'True') {
                this.displayUploadandDataSourceBlock = false;
              }
              // this.selectedTemplate = params.modelCategory;
  
              this.modelName = params.modelName;
              this.pageSource = params.pageSource;
              const oldCorrelationId = localStorage.getItem('oldCorrelationID') !== 'null' ? localStorage.getItem('oldCorrelationID') : null;
              const currentCorrelationId = this.ls.getCorrelationId();
              if (oldCorrelationId !== currentCorrelationId) {
                this.prebuiltInputAction = false;
              } else if (oldCorrelationId === currentCorrelationId) {
                this.prebuiltInputAction = true;
              }
              if (params.correlationId) {
                this.correlationId = params.correlationId;
                this.ls.setLocalStorageData('correlationId', params.correlationId);
              }
              if (params.IsTemplate) {
  
              }
              this.setData();
            }
          });
      }
      if (localStorage.getItem('oldCorrelationID') === 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6') {
        this.FMscenario = true;
      } else {
        this.FMscenario = false;
      }
    }
  
    openValidRecordDetailsPopup(ValidRecordsDetails) {
      // const message = ValidRecordsDetails['Msg'];
      const openDialogForEmptyMessage = this.dialogService.open(ValidRecordDetailsPopupComponent,
        {
          data: {
            Message: ValidRecordsDetails
          }
        }).afterClosed.pipe(
          tap(submitFlag => {
            if (submitFlag) {
              if (!this.coreUtilService.isNil(this.correlationId)) {
                this.appUtilsService.loadingStarted();
                this.adProblemStatementService.deleteModelByCorrelationId(this.correlationId).subscribe(
                  data => {
                    this.appUtilsService.loadingEnded();
                    let url = '/choosefocusarea';
                    this.router.navigateByUrl(url);
                  }, error => {
                    this.appUtilsService.loadingEnded();
                  });
              }
            }
          })
        );
      openDialogForEmptyMessage.subscribe();
  
    }
  
    // View 100 Data Points Functionality
    showData() {
      this.appUtilsService.loadingStarted();
      this.correlationId = this.ls.getCorrelationId();
      this.adProblemStatementService.getViewUploadedData(this.correlationId, this.decimalPoint).subscribe(data => {
        if (data) {
          this.tableData = data;
          this.showDataColumnsList = Object.keys(data[0]);
          this.appUtilsService.loadingEnded();
          this.dialogService.open(ShowDataComponent, {
            data: {
              'tableData': this.tableData,
              'columnList': this.showDataColumnsList,
              'problemTypeFlag': this.problemTypeFlag
            }
          });
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        this.ns.error('Something went wrong to get View Upload Data.');
      });
    }
  
    disableTabs(targetColumn: any) {
  
      // Problem Statement
      // Model Engineering
      // Data Engineering
      // Deploy Model
      // IsModelTrained: false
      // IsModelDeployed: false
  
  
      const classnameDisabled = 'anchor-disabled';
      const classnameEnabled = 'anchor-enabled';
      const nativeElemnt = !this.coreUtilService.isNil(this.eleRef.nativeElement) ? this.eleRef.nativeElement : null;
      const parentEle = !this.coreUtilService.isNil(nativeElemnt) ? nativeElemnt.parentElement : null;
      const nestedParentEle = !this.coreUtilService.isNil(parentEle) ? parentEle.parentElement : null;
  
      if (!this.coreUtilService.isNil(nestedParentEle)) {
        const allLinks = nestedParentEle.children[0].querySelectorAll('a');
        if (!this.adProblemStatementService.getColumnsdata.IsModelDeployed) {
          for (let index = 0; index < allLinks.length; index++) {
            if (index > 1) {
              // allLinks[index].className = classname;
              if (allLinks[index].text === 'Deploy Model') {
                allLinks[index].className = classnameDisabled;
              }
  
              if (!this.adProblemStatementService.getColumnsdata.IsModelTrained) {
                if (allLinks[index].text === 'Data Engineering') {
                  if (targetColumn) {
                    allLinks[index].className = classnameEnabled;
                  } else {
                    allLinks[index].className = classnameDisabled;
                  }
                }
                if (allLinks[index].text === 'Model Engineering') {
                  allLinks[index].className = classnameDisabled;
                }
              } else if (allLinks[index].text === 'Model Engineering') {
                allLinks[index].className = classnameEnabled;
              }
            }
          }
        }
      }
    }
  
    setData() {
      // this.correlationId = '1cb11b06-fffa-48cf-8e9c-35feb98f6c3e';
      localStorage.removeItem('targetColumn');
      if ((localStorage.getItem('marketPlaceRedirected')) === 'True') {
        this.selectedTemplate = this.ls.getMPModelCategory();
      } else {
        this.selectedTemplate = this.ls.getModelCategory();
      }
      if (this.pageSource === 'true') { // navigating from my models
        this.displayDataSourceLink = true;
        this.getColumnsForMyModels();
        this.showSaveAs = true;
        //  // this.setClassOnElement('saveAs', 'btn btn-primary');
        this.targetColButtonDisabled = false;
        this.uniqueIdentifierDisabled = false;
        this.timeSeriesColumnsDisabled = false;
        if (this.readOnly === 'true') {
          this.targetColButtonDisabled = true;
          this.uniqueIdentifierDisabled = true;
          this.timeSeriesColumnsDisabled = true;
          this.inputColumnsDisabled = true;
          this.showSave = false;
        }
        this.isModelSaved = true;
      } else if (this.modelName) {
        if (this.selectedTemplate && this.modelName) {
          this.isExistingTemplate = true;
          this.isNewModel = false;
          this.showSave = false;
          this.isNextDisabled.emit(true);
          const oldCorrelationId = localStorage.getItem('oldCorrelationID') !== 'null' ? localStorage.getItem('oldCorrelationID') : null;
          const currentCorrelationId = this.ls.getCorrelationId();
          if (oldCorrelationId !== currentCorrelationId) {
            this.prebuiltInputAction = false;
            this.isNextDisabled.emit(false);
          }
          if (this.adProblemStatementService.isPredefinedTemplate === 'False') {
            this.showSaveAs = true;
            this.showSave = true;
            this.showSelectAll = true;
            /* // this.setClassOnElement('save', 'btn btn-primary');
            // this.setClassOnElement('saveAs', 'btn btn-primary');
            // this.setClassOnElement('btnSelectAll', 'btn btn-primary'); */
          } else {
            // // this.setClassOnElement('save', 'btn btn-secondary btncolor');
          }
          this.targetColButtonDisabled = true;
          this.timeSeriesColumnsDisabled = true;
          this.uniqueIdentifierDisabled = true;
        } else if (this.selectedTemplate === '' && this.modelName) {
          this.isExistingTemplate = false;
          this.isNewModel = true;
          this.displayDataSourceLink = false;
        }
        if (this.isNewModel === true) {
          this.isModelSaved = false;
        } else {
          this.isModelSaved = true;
        }
        this.getColumns();
      }
      // // this.setClassOnElement('anchorUseCase', 'ingrAI-sub-nav-button active');
    }
  
    getColumns() {
      this.appUtilsService.loadingStarted();
      this.adProblemStatementService.getColumnsAD(this.correlationId, this.isExistingTemplate, this.isNewModel).subscribe(data => {

        this.isModelTrained = data.IsModelTrained;
        localStorage.setItem('targetColumn', data.TargetColumn);
        this.ls.setLocalStorageData('modelCategory', data.Category);
        this.previousModelTarget = data.PreviousModelName + '_' + data.PreviousTargetColumn;
        this.proba1Value = data.PreviousModelName + '_Proba1';
        /* added for marketplace */
        if (localStorage.getItem('registeredMPUser') === 'True') {
          localStorage.removeItem('registeredMPUser');
        }
        if (localStorage.getItem('marketPlaceRedirected') === 'True') {
          localStorage.removeItem('marketPlaceRedirected');
        }
        /* added for marketplace */
  
        if (!this.coreUtilService.isNil(data.UniquenessDetails)) {
          this.checkIfCustomTargetColumn(data.TargetColumn, Object.keys(data.UniquenessDetails));
        }
        if (this.isNewModel === true) {
          if (data.ValidRecordsDetails !== undefined && data.ValidRecordsDetails !== null) {
            this.openValidRecordDetailsPopup(data.ValidRecordsDetails);
          }
        }
  
        this.adProblemStatementService.storeData(data);
        this.disableTabs(data.TargetColumn);
        this.deliveryTypeName = data.Category;
        if (this.deliveryTypeName === 'release Management' || this.deliveryTypeName === 'AD') {
          this.deliveryTypeName = 'AD';
          this.categoryType = 'System Integration';
        } else if (this.deliveryTypeName === 'Agile Delivery' || this.deliveryTypeName === 'Agile') {
          this.deliveryTypeName = 'Agile';
          this.categoryType = 'Agile';
        } else if (this.deliveryTypeName === 'Devops') {
          this.deliveryTypeName = 'Devops';
          this.categoryType = 'Devops';
        } else if (this.deliveryTypeName === 'AIops' || this.deliveryTypeName === 'AM' || this.deliveryTypeName === 'IO') {
          this.categoryType = 'AIops';
        } else if (this.deliveryTypeName === 'PPM') {
          this.deliveryTypeName = 'PPM';
          this.categoryType = 'PPM';
        } else {
          this.categoryType = 'Others';
        }
        //  this.filterMetricData();
  
        if (data.UniquenessDetails !== undefined && data.UniquenessDetails !== null) {
          this.UniquenessDetails = data.UniquenessDetails;
        }
        if (data.IsModelDeployed === true || data.IsModelTrained === true) {
          this.targetColButtonDisabled = true;
        } else {
          this.targetColButtonDisabled = false;
        }
        if (data.ModelName) {
          this.currentModelName = data.ModelName;
        }
        if (data.ColumnsList != null || data.AvailableColumns != null) {
          if (data.ColumnsList != null) {
            const tempData = (data.ColumnsList) ? (data.ColumnsList) : '';
            this.columnData = tempData ?
              new Set(tempData) : new Set([]);
            if (data.IsEntityModel === true) {
              if (data.TargetUniqueIdentifier) {
                this.columnData.delete(data.TargetUniqueIdentifier);
              }
            }
          }
          if (this.isExistingTemplate) {
            if (data.AvailableColumns != null) {
              if (data.AvailableColumns.length >= 0) {
                const tempData = (data.AvailableColumns) ? (data.AvailableColumns) : '';
                this.columnData = tempData ?
                  new Set(tempData) : new Set([]);
              }
            }
          } else {
            if (data.AvailableColumns != null) {
              const tempData = (data.AvailableColumns) ? (data.AvailableColumns) : '';
              this.columnData = tempData ?
                new Set(tempData) : new Set([]);
            }
          }
          if (data.TargetColumn != null) {
            this.targetColumn = data.TargetColumn;
            this.oldTargetColumn = data.TargetColumn;
            this.targetAggDisable = false;
            this.adProblemStatementService.setTargetColumn(this.targetColumn);
            this.showSelectAll = true;
            //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
          } else if (data.TargetColumn === null) {
            if (this.columnData.has(this.targetColumn)) {
              this.columnData.delete(this.targetColumn);
            } else {
              this.targetColumn = '';
              this.targetAggDisable = true;
              this.adProblemStatementService.setTargetColumn(this.targetColumn);
              this.showSelectAll = false;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
            }
          }
  
          if (data.InputColumns) {
            const tempinputColums = data ? data.InputColumns : '';
            this.inputColumns = data.InputColumns ? new Set(tempinputColums) : new Set([]);
          } else if (data.InputColumns === null) {
            this.inputColumns = new Set([]);
          }
  
          if (data.hasOwnProperty('IsIncludedinCustomCascade')) {
            if (data.IsIncludedinCustomCascade === true) {
              this.isIncludedinCustomCascade = true;
              if (data.hasOwnProperty('CascadeModelsCount')) {
                this.cascadeModelCount = data.CascadeModelsCount;
              }
            } else {
              this.isIncludedinCustomCascade = false;
              this.cascadeModelCount = 0;
            }
          } else {
            this.isIncludedinCustomCascade = false;
            this.cascadeModelCount = 0;
          }
  
          if (data.hasOwnProperty('CustomCascadeId')) {
            this.customCascadedId = data.CustomCascadeId;
          }
  
          if (data.TargetUniqueIdentifier) {
            this.uniqueIdentifierDisabled = false;
            this.uniqueIdentifierVal = data.TargetUniqueIdentifier;
            this.oldUniqueIdentifierVal = data.TargetUniqueIdentifier;
            this.adProblemStatementService.setUniqueIdentifier(this.uniqueIdentifierVal);
          } else if (data.TargetUniqueIdentifier === null) {
            this.uniqueIdentifierVal = '';
            this.uniqueIdentifierDisabled = false;
            this.adProblemStatementService.setUniqueIdentifier(this.uniqueIdentifierVal);
          }
  
          if (data.BusinessProblems) {
            this.problemStatement = data.BusinessProblems;
          } else if (data.BusinessProblems === null) {
            this.problemStatement = '';
          }
  
          if (data.DataTypeColumns) {
            this.columnDataTypes = data.DataTypeColumns;
          }
  
          if (data.Aggregation) {
            this.targetAggVal = data.Aggregation;
            this.isTimeSeriesOn = true;
            if (this.isTimeSeriesOn) {
              this.showSelectAll = false;
              this.hideTargetValForNone = false;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
            } else {
              this.showSelectAll = true;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
            }
          } else if (data.Aggregation === null) {
            this.targetAggVal = '';
            this.isTimeSeriesOn = false;
          }
          if (data.FrequencyList) {
            // this.timeSeriesCheckboxValues = data.FrequencyList;
            // this.timeSeriesCheckboxValues = this.timeSeriesCheckboxValues1;
            const freq = data.FrequencyList.Frequency;
            for (let a = 0; a < Object.keys(freq).length; a++) {
              if (data.FrequencyList.Frequency[a]['Steps'] !== null) {
                this.timeSeriesCheckboxValues1[a]['Steps'] = data.FrequencyList.Frequency[a]['Steps'];
              }
              if (data.FrequencyList.Frequency[a].hasOwnProperty('value')) {
                this.timeSeriesCheckboxValues1[a]['value'] = data.FrequencyList.Frequency[a]['value'];
              }
            }
            this.timeSeriesCheckboxValues = this.timeSeriesCheckboxValues1;
          } else if (data.FrequencyList === null) {
            this.timeSeriesCheckboxValues = {
              '0': {
                'Name': 'Hourly',
                'Steps': null
              },
              '1': {
                'Name': 'Daily',
                'Steps': null
              },
              '2': {
                'Name': 'Weekly',
                'Steps': null
              },
              '3': {
                'Name': 'Monthly',
                'Steps': null
              },
              '4': {
                'Name': 'Fortnightly',
                'Steps': null
              },
              '5': {
                'Name': 'Quarterly',
                'Steps': null
              },
              '6': {
                'Name': 'Half-Year',
                'Steps': null
              },
              '7': {
                'Name': 'Yearly',
                'Steps': null
              },
              '8': {
                'Name': 'CustomDays',
                'Steps': null,
                'value': null
              }
            };
          }
  
          if (data.TimeSeriesColumn) {
            this.timeSeriesColumns = data.TimeSeriesColumn;
            this.adProblemStatementService.setTimeSeriesColumn(this.timeSeriesColumns);
          } else if (data.TimeSeriesColumn === null) {
            this.timeSeriesColumns = '';
            this.adProblemStatementService.setTimeSeriesColumn(this.timeSeriesColumns);
          }
          if (data.InstaFLag === true) {
            this.disableAllInputs();
          }
  
          if (data.IsModelDeployed === true && data.TimeSeriesColumn !== null) {
            this.isTimeSeriesOn = true;
          } else if (data.IsModelDeployed === true && data.TimeSeriesColumn === null) {
            this.isTimeSeriesOn = false;
          }
  
          this.appUtilsService.loadingEnded();
        } else {
          this.ns.error('Records are coming as null from database for selected model -' + this.modelName);
          this.appUtilsService.loadingEnded();
        }
        sessionStorage.setItem('isModelDeployed', data.IsModelDeployed)
        if (!this.coreUtilService.isNil(this.isNewModel)) {
          sessionStorage.setItem('isNewModel', this.isNewModel.toString());
        }
        sessionStorage.setItem('isModelTrained', data.IsModelTrained);
        this.coreUtilService.disableADTabs(this.currentIndex, this.breadcrumbIndex);
        if (data.IsFMModel === true) {
          this.isFMModel = true;
          this.showSelectAll = false;
          this.isNextDisabled.emit(true);
          this.coreUtilService.allTabs[1].status = 'disabled';
          this.coreUtilService.allTabs[2].status = 'disabled';
          this.coreUtilService.allTabs[3].status = ''; // Enable Deploy Model Tab
          this.targetColButtonDisabled = true;
          this.uniqueIdentifierDisabled = true;
          this.columnData.delete(this.targetColumn);
          this.columnData.delete(this.uniqueIdentifierVal);
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        this.ns.error('Something went wrong to get getColumns ');
      });
    }
  
    getColumnsForMyModels() {
      this.appUtilsService.loadingStarted();
      this.adProblemStatementService.getColumnsForMyModels(this.correlationId).subscribe(data => {
  
        this.isModelTrained = data.IsModelTrained;
        this.ls.setLocalStorageData('modelCategory', data.Category);
        this.previousModelTarget = data.PreviousModelName + '_' + data.PreviousTargetColumn;
        this.proba1Value = data.PreviousModelName + '_Proba1';
  
        if (data.ColumnsList != null || data.AvailableColumns != null) {
          localStorage.setItem('targetColumn', data.TargetColumn);
          this.checkIfCustomTargetColumn(data.TargetColumn, Object.keys(data.UniquenessDetails));
          this.adProblemStatementService.storeData(data);
          this.disableTabs(data.TargetColumn);
          this.deliveryTypeName = data.Category;
          if (this.deliveryTypeName === 'release Management' || this.deliveryTypeName === 'AD') {
            this.deliveryTypeName = 'AD';
            this.categoryType = 'System Integration';
          } else if (this.deliveryTypeName === 'Agile' || this.deliveryTypeName === 'Agile') {
            this.deliveryTypeName = 'Agile';
            this.categoryType = 'Agile';
          } else if (this.deliveryTypeName === 'Devops') {
            this.deliveryTypeName = 'Devops';
            this.categoryType = 'Devops';
          } else if (this.deliveryTypeName === 'AIops' || this.deliveryTypeName === 'AM' || this.deliveryTypeName === 'IO') {
            this.categoryType = 'AIops';
          } else if (this.deliveryTypeName === 'PPM') {
            this.deliveryTypeName = 'PPM';
            this.categoryType = 'PPM';
          } else {
            this.categoryType = 'Others';
          }
          //  this.filterMetricData();
  
          if (data.UniquenessDetails !== undefined && data.UniquenessDetails !== null) {
            this.UniquenessDetails = data.UniquenessDetails;
          }
  
          if (data.IsModelDeployed === true || data.IsModelTrained === true) {
            this.targetColButtonDisabled = true;
            this.iFModelDeployed = data.IsModelDeployed; this.iFModelTrained = data.IsModelTrained;
            sessionStorage.setItem('isModelDeployed', data.IsModelDeployed)
          }
  
          if (data.hasOwnProperty('IsIncludedinCustomCascade')) {
            if (data.IsIncludedinCustomCascade === true) {
              this.isIncludedinCustomCascade = true;
              if (data.hasOwnProperty('CascadeModelsCount')) {
                this.cascadeModelCount = data.CascadeModelsCount;
              }
            } else {
              this.isIncludedinCustomCascade = false;
              this.cascadeModelCount = 0;
            }
          } else {
            this.isIncludedinCustomCascade = false;
            this.cascadeModelCount = 0;
          }
  
          if (data.hasOwnProperty('CustomCascadeId')) {
            this.customCascadedId = data.CustomCascadeId;
          }
  
          if (data.InputColumns) {
            const tempinputColums = data ? data.InputColumns : '';
            this.inputColumns = data.InputColumns ? new Set(tempinputColums) : new Set([]);
          }
          if (data.TargetUniqueIdentifier) {
            this.uniqueIdentifierVal = data.TargetUniqueIdentifier;
            this.oldUniqueIdentifierVal = data.TargetUniqueIdentifier;
            this.adProblemStatementService.setUniqueIdentifier(this.uniqueIdentifierVal);
          }
          if (data.DataTypeColumns) {
            this.columnDataTypes = data.DataTypeColumns;
          }
          if (data.Aggregation) {
            this.targetAggVal = data.Aggregation;
            this.isTimeSeriesOn = true;
            if (this.isTimeSeriesOn) {
              this.showSelectAll = false;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
            } else {
              this.showSelectAll = true;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
  
            }
          }
          if (data.FrequencyList) {
            // this.timeSeriesCheckboxValues = this.timeSeriesCheckboxValues1; // data.FrequencyList;
            this.dataFrequency = data.FrequencyList.Frequency;
            const freq = data.FrequencyList.Frequency;
            for (let a = 0; a < Object.keys(freq).length; a++) {
              if (data.FrequencyList.Frequency[a]['Steps'] !== null) {
                this.timeSeriesCheckboxValues1[a]['Steps'] = data.FrequencyList.Frequency[a]['Steps'];
              }
              if (data.FrequencyList.Frequency[a].hasOwnProperty('value')) {
                this.timeSeriesCheckboxValues1[a]['value'] = data.FrequencyList.Frequency[a]['value'];
              }
            }
            this.timeSeriesCheckboxValues = this.timeSeriesCheckboxValues1;
            ///  console.log('checkboxvalues POST --', this.timeSeriesCheckboxValues);
          }
          if (data.TimeSeriesColumn) {
            this.timeSeriesColumns = data.TimeSeriesColumn;
            this.timeSeriesColumnsDisabled = false;
            if (this.readOnly === 'true') {
              this.targetColButtonDisabled = true;
              this.uniqueIdentifierDisabled = true;
              this.timeSeriesColumnsDisabled = true;
              this.inputColumnsDisabled = true;
              this.showSave = false;
            }
            this.adProblemStatementService.setTimeSeriesColumn(this.timeSeriesColumns);
          }
  
          if (data.TargetColumn) {
            this.targetColumn = data.TargetColumn;
            this.oldTargetColumn = data.TargetColumn;
            this.adProblemStatementService.setTargetColumn(this.targetColumn);
            if (this.isTimeSeriesOn) {
              this.targetAggDisable = false;
              this.showSelectAll = false;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
            } else {
              this.showSelectAll = true;
              //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
            }
  
          }
          if (data.AvailableColumns) {
            const tempData = (data.AvailableColumns) ? (data.AvailableColumns) : '';
            this.columnData = tempData ?
              new Set(tempData) : new Set([]);
          }
          if (!data.AvailableColumns && data.ColumnsList) {
            const tempData = (data.ColumnsList) ? (data.ColumnsList) : '';
            this.columnData = tempData ?
              new Set(tempData) : new Set([]);
            if (data.IsEntityModel === true) {
              if (data.TargetUniqueIdentifier) {
                this.columnData.delete(data.TargetUniqueIdentifier);
              }
            }
          }
          if (data.BusinessProblems) {
            this.problemStatement = data.BusinessProblems;
          }
          if (data.DataSource) {
            this.dataSourceName = data.DataSource;
          }
          if (data.ModelName) {
            this.currentModelName = data.ModelName;
          }
          if (this.isTimeSeriesOn) {
            this.showSelectAll = false;
            // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
          } else {
            this.showSelectAll = true;
            // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
          }
          if (data.InstaFLag === true) {
            this.disableAllInputs();
          }
          // this.timeSeriesToggle();
          this.checkTimeSeriesStatus();
          this.dataTypeVal(this.targetColumn);
          // sessionStorage.setItem('isModelDeployed', data.IsModelDeployed)
          if (!this.coreUtilService.isNil(this.isNewModel)) {
            sessionStorage.setItem('isNewModel', this.isNewModel.toString());
          }
          sessionStorage.setItem('isModelTrained', data.IsModelTrained);
          this.coreUtilService.disableADTabs(this.currentIndex, this.breadcrumbIndex);
          this.appUtilsService.loadingEnded();
        } else {
          this.ns.error('Records are coming as null from database for selected model -' + this.modelName);
          this.appUtilsService.loadingEnded();
        }
        if (data.IsFMModel === true) {
          this.isFMModel = true;
          this.showSelectAll = false;
          this.isNextDisabled.emit(true);
          this.coreUtilService.allTabs[1].status = 'disabled';
          this.coreUtilService.allTabs[2].status = 'disabled';
          this.coreUtilService.allTabs[3].status = ''; // Enable Deploy Model Tab
          this.targetColButtonDisabled = true;
          this.uniqueIdentifierDisabled = true;
          this.columnData.delete(this.targetColumn);
          this.columnData.delete(this.uniqueIdentifierVal);
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        this.ns.error('Something went wrong to get getColumns ');
      });
    }
  
    onDrag(event, from, columnName) {
      if (this.isIE) {
        if (from === 'list') {
          event.dataTransfer.setData('Text', 'listContainer#' + columnName);
        }
        if (from === 'columnsContainer') {
          event.dataTransfer.setData('Text', 'columnsContainer#' + columnName);
        }
      } else {
        if (from === 'list') {
          event.dataTransfer.setData('textFromColumnlist', columnName);
        }
        if (from === 'columnsContainer') {
          event.dataTransfer.setData('textfromInputContainer', columnName);
        }
      }
    }
  
    allowDrop(event) {
      event.preventDefault();
    }
  
    removeColumn(column, from) {
      if (from === 'targetColumn') {
        this.targetColumn = '';
        if (!this.customTargetColumn.has(column)) {
          this.columnData.add(column);
        } else {
          this.showCustomTarget = true;
        }
        this.targetAggDisable = true;
        this.showSelectAll = false;
        // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
      }
      if (from === 'uniqueIdentifier') {
        this.uniqueIdentifierVal = '';
        this.columnData.add(column);
      }
      if (from === 'inputColumns') {
        this.inputColumns.delete(column);
        if (!this.customTargetColumn.has(column)) {
          this.columnData.add(column);
        } else {
          this.showCustomTarget = true;
        }
      }
      if (from === 'timeSeriesColumns') {
        this.timeSeriesColumns = '';
        this.columnData.add(column);
      }
    }
  
    onSelectChange(value, from, to, event?) {
      this.isDirty = 'Yes';
      if (sessionStorage.getItem('isModelDeployed') === 'true' && (to === "uniqueIdentifierContainer")) {
        this.isNextDisabled.emit(true);
      }
  
      let textFromColumnlist = '';
      let textfromInputContainer = '';
      if (from === 'columnsContainer') {
        textFromColumnlist = value;
      } else if (from === 'list') {
        textfromInputContainer = value;
      }
      if (value === 'CustomTarget') {
        this.oldTargetColumn = this.targetColumn;
        this.targetColumn = 'CustomTarget';
      }
      if (to === 'targetContainer') {
        this.latestTargetValue = textFromColumnlist;
        let dataType;
        if (textFromColumnlist !== '') {
          if (this.isTimeSeriesOn) {
            this.targetAggDisable = false;
            dataType = this.checkTargetColumnDataType(textFromColumnlist);
            this.dataTypeVal(textFromColumnlist);
            if (dataType) {
              if (!this.coreUtilService.isNil(this.targetColumn)) {
                if (this.targetColumn === 'CustomTarget') {
                  this.targetColumn = this.oldTargetColumn;
                  this.isCustomAttrClicked = true;
                }
                this.columnData.add(this.targetColumn);
              }
              this.targetColumn = textFromColumnlist;
              this.columnData.delete(this.targetColumn);
              this.showSelectAll = false;
              // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
            } else {
              this.ns.error('Target Column should  be of Numeric datatype');
            }
          } else {
            if(this.isOnlyNumericType(textFromColumnlist)){
            this.targetAggDisable = true;
            if (!this.coreUtilService.isNil(this.targetColumn)) {
              if (this.targetColumn === 'CustomTarget') {
                this.targetColumn = this.oldTargetColumn;
                this.isCustomAttrClicked = true;
              }
              if (this.customTargetColumn.has(this.targetColumn)) {
                this.showCustomTarget = true;
              } else {
                if (this.targetColumn !== '') {
                  this.columnData.add(this.targetColumn);
                }
              }
            }
            this.targetColumn = textFromColumnlist;
            this.columnData.delete(this.targetColumn);
            this.inputColumns.delete(this.targetColumn);
            if (this.customTargetColumn.has(textFromColumnlist)) { this.showCustomTarget = false; }
            this.showSelectAll = true;
          }else{
            this.ns.error('Target Column should be of Numeric datatype');
          }
          }
  
        }
        if (textfromInputContainer !== '') {
          if (this.isTimeSeriesOn) {
            this.targetAggDisable = false;
            dataType = this.checkTargetColumnDataType(textFromColumnlist);
            this.dataTypeVal(textFromColumnlist);
            if (dataType) {
              if (!this.coreUtilService.isNil(this.targetColumn)) {
                this.inputColumns.add(this.targetColumn);
              }
              this.targetColumn = textfromInputContainer;
              this.inputColumns.delete(this.targetColumn);
              this.showSelectAll = false;
              // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
            } else {
              this.ns.error('Target Column should  be of Numeric datatype');
            }
          } else {
            this.targetAggDisable = true;
            if (!this.coreUtilService.isNil(this.targetColumn)) {
              if (this.customTargetColumn.has(this.targetColumn)) { this.showCustomTarget = true; }
              else { this.inputColumns.add(this.targetColumn); }
            }
            this.targetColumn = textfromInputContainer;
            this.inputColumns.delete(this.targetColumn);
            this.showSelectAll = true;
            // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
          }
  
        }
      }
      if (to === 'uniqueIdentifierContainer') {
        if (textFromColumnlist !== '') {
          // if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
          //   this.columnData.add(this.uniqueIdentifierVal);
          // }
          if (this.UniquenessDetails !== undefined && this.UniquenessDetails !== null) {
            const checkpercentagemessage = this.UniquenessDetails;
            if (checkpercentagemessage.hasOwnProperty(textFromColumnlist)) {
              if (checkpercentagemessage[textFromColumnlist]['Percent'] >= 90.0 &&
                checkpercentagemessage[textFromColumnlist]['Percent'] < 100.0) {
                if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
                  this.columnData.add(this.uniqueIdentifierVal);
                  this.inputColumns.delete(this.uniqueIdentifierVal);
                }
                const message = checkpercentagemessage[textFromColumnlist]['Message'];
                this.uniqueIdentifierVal = textFromColumnlist;
                this.columnData.delete(this.uniqueIdentifierVal);
                this.inputColumns.delete(this.uniqueIdentifierVal);
                this.ns.warning(message);
              } else if (checkpercentagemessage[textFromColumnlist]['Percent'] === 100.0) {
                if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
                  this.columnData.add(this.uniqueIdentifierVal);
                  this.inputColumns.delete(this.uniqueIdentifierVal);
                }
                this.uniqueIdentifierVal = textFromColumnlist;
                this.columnData.delete(this.uniqueIdentifierVal);
                this.inputColumns.delete(this.uniqueIdentifierVal);
              } else if (checkpercentagemessage[textFromColumnlist]['Percent'] < 90.0) {
                const message = 'Unique Identifier must have more than 90% unique values in the data';
                this.ns.warning(message);
              }
            }
          }
        }
        if (textfromInputContainer !== '') {
          if (this.UniquenessDetails !== undefined && this.UniquenessDetails !== null) {
            if (textfromInputContainer !== undefined) {
              const checkpercentagemessage = this.UniquenessDetails;
              if (checkpercentagemessage.hasOwnProperty(textfromInputContainer)) {
                if (checkpercentagemessage[textfromInputContainer]['Percent'] >= 90.0 &&
                  checkpercentagemessage[textfromInputContainer]['Percent'] < 100.0) {
                  if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
                    this.inputColumns.add(this.uniqueIdentifierVal);
                  }
                  const message = checkpercentagemessage[textfromInputContainer]['Message'];
                  this.uniqueIdentifierVal = textfromInputContainer;
                  this.columnData.delete(this.uniqueIdentifierVal);
                  this.inputColumns.delete(this.uniqueIdentifierVal);
                  if (!this.coreUtilService.isNil(this.oldUniqueIdentifierVal))
                    this.columnData.add(this.oldUniqueIdentifierVal);
                  this.ns.warning(message);
                } else if (checkpercentagemessage[textfromInputContainer]['Percent'] === 100.0) {
                  if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
                    this.inputColumns.add(this.uniqueIdentifierVal);
                  }
                  this.uniqueIdentifierVal = textfromInputContainer;
                  this.columnData.delete(this.uniqueIdentifierVal);
                  this.inputColumns.delete(this.uniqueIdentifierVal);
                  if (!this.coreUtilService.isNil(this.oldUniqueIdentifierVal))
                    this.columnData.add(this.oldUniqueIdentifierVal);
                } else if (checkpercentagemessage[textfromInputContainer]['Percent'] < 90.0) {
                  this.columnData.delete(this.uniqueIdentifierVal);
                  this.inputColumns.delete(this.uniqueIdentifierVal);
                  if (!this.coreUtilService.isNil(this.oldUniqueIdentifierVal))
                    this.columnData.add(this.oldUniqueIdentifierVal);
                  const message = 'Unique Identifier must have more than 90% unique values in the data';
                  this.ns.warning(message);
                }
              }
            } else {
              if (this.isTimeSeriesOn) {
                if (!this.coreUtilService.isNil(this.oldUniqueIdentifierVal))
                  this.columnData.add(this.oldUniqueIdentifierVal);
              }
            }
          }
          this.oldUniqueIdentifierVal = this.uniqueIdentifierVal;
        }
      }
      if (to === 'columnsContainer') {
        if (value !== this.proba1Value && value !== this.previousModelTarget) {
          if (!this.customTargetColumn.has(textFromColumnlist)) {
            console.log(event.currentTarget.checked);
            if (event.currentTarget.checked === true) {
              this.inputColumns.add(textFromColumnlist);
              this.columnData.delete(textFromColumnlist);
            } else {
              this.removeColumn(textFromColumnlist, 'inputColumns');
            }
          }
        } else {
          event.target.checked = true;
        }
      }
      if (to === 'timeSeriesColumnsContainer') {
        if (!this.coreUtilService.isNil(textFromColumnlist)) {
          const dateDatatype = this.checkDataType(textFromColumnlist);
          if (dateDatatype) {
            if (textFromColumnlist !== '') {
              if (!this.coreUtilService.isNil(this.timeSeriesColumns)) {
                this.columnData.add(this.timeSeriesColumns);
              }
              this.timeSeriesColumns = textFromColumnlist;
              this.columnData.delete(this.timeSeriesColumns);
              this.inputColumns.delete(this.timeSeriesColumns);
            }
            if (textfromInputContainer !== '') {
              if (!this.coreUtilService.isNil(this.timeSeriesColumns)) {
                this.inputColumns.add(this.timeSeriesColumns);
              }
              this.timeSeriesColumns = textfromInputContainer;
              this.columnData.delete(this.timeSeriesColumns);
              this.inputColumns.delete(this.timeSeriesColumns);
            }
          } else {
            event.currentTarget.checked = false;
            this.ns.error('DataType for influencing attribute in Time Series Model should be Date');
          }
        } else {
          event.currentTarget.checked = false;
        }
      }
      this.checkForTimeSeries();
    }
  
    checkDataType(columnName) {
      const dataType = this.columnDataTypes[columnName];
      if (dataType === 'Date') {
        return true;
      } else {
        return false;
      }
    }
  
    checkTargetColumnDataType(columnName) {
      return this.isOnlyNumericType(columnName);
    }

    dataTypeVal(columnName) {
      const dataType = this.columnDataTypes[columnName];
      if (dataType === 'Float' || dataType === 'Integer') {
        this.targetAggregation = ['None', 'Sum', 'Product', 'Mean', 'Median', 'Count', 'Max', 'Min'];
      } else if (dataType === 'Category') {
        this.targetAggregation = ['None', 'LabelCounts'];
      }
    }
  
    isOnlyNumericType(columnName){
      const dataType = this.columnDataTypes[columnName];
      if (dataType === 'Float' || dataType === 'Integer') {
        return true;
      }else{
        return false;
      }
    }
  
    selectAll() {
      this.isDirty = 'Yes';
      this.toggleSelect = !this.toggleSelect;
      this.columnData.forEach(column => {
        this.inputColumns.add(column);
      });
      this.columnData.clear();
    }
  
    deselectAll() {
      this.isDirty = 'Yes';
      this.toggleSelect = !this.toggleSelect;
      this.inputColumns.forEach(column => {
        this.columnData.add(column);
      });
      this.inputColumns.clear();
    }
  
    moveToTarget(column) {
      const _this = this;
      _this.targetColumn = column;
      _this.showSelectAll = true;
      _this.inputColumns.delete(column);
      _this.columnData.delete(column);
    }
  
    saveAs() {
      this.uts.usageTracking('Problem Statement', 'Save As');
      const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
        switchMap(modelName => {
          this.newModelName = modelName;
          if (modelName !== null && modelName !== '' && modelName !== undefined) {
            return this.adProblemStatementService.cloneAD(this.correlationId, modelName);
          } else {
            // tslint:disable-next-line: deprecation
            return empty();
          }
        }),
  
        tap(data => this.newCorrelationIdAfterCloned = data),
  
        tap(data => {
  
          if (data) {
            this.navigateToUseCaseDifination(data[0]);
            this.adProblemStatementService.isPredefinedTemplate = 'False';
            this.ns.success('Cloned Successfully');
          }
        })
      );
  
      openTemplateAfterClosed.subscribe();
    }
  
    save(isNextClicked?) {
      if (!this.readOnly) {
        const checkpercentagemessage = this.UniquenessDetails;
        if (checkpercentagemessage.hasOwnProperty(this.uniqueIdentifierVal)) {
          if (checkpercentagemessage[this.uniqueIdentifierVal]['Percent'] < 90.0) {
            const message = 'Unique identifier ' + this.uniqueIdentifierVal + ' unique percent should not be less than 90%';
            this.ns.warning(message);
            return 0;
          }
        }
        this.isDirty = 'No';
        if (this.readOnly !== 'true') {
          const _this = this;
          let correlationIdToSave = '';
          let successMsg = '';
          this.validationForDropDownVal = false;
          this.targetAggregationVal = false;
          this.uts.usageTracking('Problem Statement', 'Save');
          if (this.selectedTemplate === null || this.selectedTemplate === undefined || this.selectedTemplate === '') {
  
            correlationIdToSave = this.correlationId;
            successMsg = 'Data Saved Successfully.';
            // this.coreUtilService.disableADTabs((this.currentIndex + 1), 0);
          } else {
            // Bug 737375 Ingrain_StageTest_R2.1: -[Prebuilt]- getting 'TeachtestData' error in the use case definition page.
            // Fix : As columnMappingCorrelationId to set only after Feature Mapping
            // so added null check
            if (this.columnMappingCorrelationId === null || this.columnMappingCorrelationId === undefined ||
              this.columnMappingCorrelationId === '') {
              successMsg = 'Data Saved Successfully.';
              correlationIdToSave = this.ls.getCorrelationId();
            } else {
              successMsg = 'Mapped Data Saved Successfully.';
              correlationIdToSave = this.columnMappingCorrelationId;
            }
            // successMsg = 'Mapped Data Saved Successfully.';
          }
          if (this.targetColumn && this.isValidate(this.problemStatement) && this.isDataValid(true)) {
            if (this.isTimeSeriesOn) {
              /* if (this.coreUtilService.isNil(this.uniqueIdentifierVal)
                && this.targetAggVal !== 'None') {
                this.ns.error('Select Unique Identifier Column');
                return;
              } */
              this.timeSeries.get('targetAggregationControl').setValidators([Validators.required]);
              let countStepsVal = 0;
              for (const field in this.timeSeries.controls) {
                if (!this.coreUtilService.isNil(field)) {
                  const control = this.timeSeries.get(field);
                  if (control !== this.timeSeries.get('timeSeriesSelect') && control !== this.timeSeries.get('targetAggregationControl') && control !== this.timeSeries.get('customDaysValueControl')) {
                    if (control.value !== '' && control.value !== null && control.value !== undefined) {
                      this.validationForDropDownVal = true;
                      countStepsVal++;
                    }
                  }
                  if (control === this.timeSeries.get('targetAggregationControl')) {
                    if (control.value !== '' && control.value !== null) {
                      this.targetAggregationVal = true;
                    }
                  }
                }
              }
              if (this.targetAggVal === 'None' && countStepsVal > 1) {
                this.ns.warning('Kindly select only one Frequency if Target Aggregation is selected to None');
                return;
              }
              if (countStepsVal > 3) {
                this.ns.warning('Kindly select only upto 3 Frequencies.');
                return;
              }
              if (this.validationForDropDownVal && this.targetAggregationVal && this.timeSeriesColumns !== '') {
                if (this.timeSeriesCheckboxValues[8].value === '') {
                  this.timeSeriesCheckboxValues[8].value = null;
                }
                const customDaysValue = this.timeSeriesCheckboxValues[8].value;
                const customDaysSteps = this.timeSeriesCheckboxValues[8].Steps;
                if ((customDaysValue !== null && customDaysValue > 0 && customDaysValue <= 31 && customDaysSteps !== null) || (customDaysValue === null && customDaysSteps === null)) {
                  this.appUtilsService.loadingStarted();
                  this.problemType = 'TimeSeries';
                  this.timeSeriesObject['Frequency'] = this.timeSeriesCheckboxValues;
                  this.timeSeriesObject['Aggregation'] = this.targetAggVal;
                  this.timeSeriesObject['TimeSeriesColumn'] = this.timeSeriesColumns;
  
                  this.adProblemStatementService.invokePython(this.columnData, this.targetColumn, this.inputColumns, correlationIdToSave, this.problemStatement,
                    this.problemType, this.timeSeriesObject, this.uniqueIdentifierVal, this.userid)
                    .subscribe(
                      data => {
                        this.ns.success(successMsg);
                        this.adProblemStatementService.isPredefinedTemplate = 'False';
                        this.isModelSaved = true;
                        localStorage.removeItem('targetColumn');
                        this.disableTabs(this.targetColumn);
                        /* _this.eleRef.nativeElement.parentElement.parentElement.querySelectorAll('.ingrAI-sub-nav')[0]
                          .lastElementChild.querySelectorAll('.btn-primary')[1].className = 'btn btn-primary bt-bg-color'; */
                        this.appUtilsService.loadingEnded();
                        this.showSaveAs = true;
                        // this.setClassOnElement('saveAs', 'btn btn-primary');
                        this.adProblemStatementService.setTargetColumn(this.targetColumn);
                        this.adProblemStatementService.setUniqueIdentifier(this.uniqueIdentifierVal);
                        if (isNextClicked){
                          this.getDataCleanUpStatus();
                          //this.redirectToNext();
                        }
                      }, error => {
                        this.appUtilsService.loadingEnded();
                        this.ns.error('Something went wrong.');
                      }
                    );
                } else {
                  if (customDaysValue !== null && (customDaysValue < 1 || customDaysValue > 30)) {
                    this.timeSeries.setErrors({ 'incorrect': true });
                    this.ns.error('Kindly enter custom days value between 1 to 30.');
                  } else if ((customDaysValue !== null && customDaysSteps === null) || (customDaysValue === null && customDaysSteps !== null)) {
                    this.timeSeries.setErrors({ 'incorrect': true });
                    this.ns.error('Kindly select value for custom days from dropdown and enter value in the textbox.');
                  }
                }
              } else if (!this.validationForDropDownVal) {
                this.timeSeries.setErrors({ 'incorrect': true });
                this.ns.error('Kindly select atleast one appropriate frequency for Time Series Forecasting');
              } else if (!this.targetAggregationVal) {
                this.timeSeries.setErrors({ 'incorrect': true });
                this.ns.error('Kindly select target aggregation');
              } else if (this.timeSeriesColumns === '') {
                this.timeSeries.setErrors({ 'incorrect': true });
                this.ns.error('Kindly Enter Time Series Column');
              }
            } else {
              if (this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
                this.ns.error('Select Unique Identifier');
                return;
              }
              if (this.inputColumns.size > 0) {
                this.appUtilsService.loadingStarted();
                //this.problemType = 'File';
                this.problemType = 'Regression';
                this.timeSeriesObject = {};
                this.adProblemStatementService.invokePython(this.columnData, this.targetColumn, this.inputColumns, correlationIdToSave,
                  this.problemStatement, this.problemType, this.timeSeriesObject, this.uniqueIdentifierVal, this.userid ,this.customTargetColumn.has(this.targetColumn))
                  .subscribe(
                    data => {
                      this.ns.success(successMsg);
                      this.adProblemStatementService.isPredefinedTemplate = 'False';
                      this.isModelSaved = true;
                      localStorage.removeItem('targetColumn');
                      this.disableTabs(this.targetColumn);
                      /* _this.eleRef.nativeElement.parentElement.parentElement.querySelectorAll('.ingrAI-sub-nav')[0]
                        .lastElementChild.querySelectorAll('.btn-primary')[1].className = 'btn btn-primary bt-bg-color'; */
                      this.appUtilsService.loadingEnded();
                      this.showSaveAs = true;
                      // this.setClassOnElement('saveAs', 'btn btn-primary');
                      this.adProblemStatementService.setTargetColumn(this.targetColumn);
                      this.adProblemStatementService.setUniqueIdentifier(this.uniqueIdentifierVal);
                      if (isNextClicked){
                        this.getDataCleanUpStatus();
                        //this.redirectToNext();
                      }
                    }, error => {
                      this.appUtilsService.loadingEnded();
                      this.ns.error('Something went wrong.');
                    }
                  );
              } else if (this.inputColumns.size === 0) {
                this.ns.error('Select Potential Influencing Attribute');
              }
  
            }
  
          } else if (!this.targetColumn) {
            this.ns.error('Select Attribute To Predict');
          } else if (!this.uniqueIdentifierVal && this.targetAggVal !== 'None' && !this.isTimeSeriesOn) {
  
            this.ns.error('Select Unique Identifier');
  
          } else if (this.inputColumns.size === 0 && this.isTimeSeriesOn === false) {
            this.ns.error('Select Potential Influencing Attribute');
          } else if (!this.problemStatement) {
            this.ns.error('Please define your Problem Statement');
          }
        } else {
          this.accessDenied();
        }
      }
    }
  
    getFileUploadDetails(e) {
      this.files = [];
      for (let i = 0; i < e.target.files.length; i++) {
        this.files.push(e.target.files[i]);
      }
      this.openTempateNameDialog(this.files);
    }
  
    openTempateNameDialog(filesData) {
      this.Myfiles = filesData;
      const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
        tap(templateName => templateName ? this.openFileProgressDialog(filesData, templateName, '') : '')
      );
      openTemplateAfterClosed.subscribe();
    }
  
    openFileProgressDialog(filesData, templateName, correlationId) {
      const totalSourceCount = filesData[0].sourceTotalLength;
      if (this.currentModelName === undefined) {
        this.currentModelName = templateName;
        this.currentModel.emit(templateName);
      }
      if (correlationId === '' || correlationId === undefined) {
        if (totalSourceCount > 1) {
          const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: templateName, correlationId: correlationId } }).afterClosed
            .pipe(
              tap(data => data ? this.openModalMultipleDialog(data.body, templateName, filesData, data.dataForMapping, correlationId, true) : '')
            );
          openDataSourceFileProgressAfterClosed.subscribe();
        } else {
          const openFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: templateName, correlationId: correlationId } }).afterClosed
            .pipe(
              tap(data => data ? this.openFeatureMappingPopup(this.columnData, data[0]) : '')
            );
          openFileProgressAfterClosed.subscribe();
        }
      } else {
        if (totalSourceCount > 1) {
          const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: templateName, correlationId: correlationId } }).afterClosed
            .pipe(
              tap(data => data ? this.openModalMultipleDialog(data.body, templateName, filesData, data.dataForMapping) : '')
            );
          openDataSourceFileProgressAfterClosed.subscribe();
        } else {
          const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: templateName, correlationId: correlationId } }).afterClosed
            .pipe(
              tap(data => {
  
                data ? this.navigateToUseCaseDefinition(data.body[0], this.modelName) : ''
              })
            );
          openDataSourceFileProgressAfterClosed.subscribe();
        }
      }
  
    }
  
    /* Multiple File Mapping Dialog */
    openModalMultipleDialog(filesData, modelName, fileUploadData, fileGeneralData, correlationId?, isPrebuiltTempalate?) {
      this.modelName = modelName;
      if (isPrebuiltTempalate) {
        if (filesData.Flag === 'flag3' || filesData.Flag === 'flag4') {
          const openFileProgressAfterClosed = this.dialogService.open(FilesMappingModalComponent,
            { data: { filesData: filesData, fileUploadData: fileUploadData, fileGeneralData: fileGeneralData } }).afterClosed.pipe(
              tap(data => data ? this.openFeatureMappingPopup(this.columnData, data[0]) : '')
            );
          openFileProgressAfterClosed.subscribe();
        } else {
          this.openFeatureMappingPopup(this.columnData, filesData.CorrelationId);
        }
      } else {
        if (filesData.Flag === 'flag3' || filesData.Flag === 'flag4') {
          const openFileMappingTemplateAfterClosed = this.dialogService.open(FilesMappingModalComponent,
            { data: { filesData: filesData, fileUploadData: fileUploadData, fileGeneralData: fileGeneralData } }).afterClosed.pipe(
              tap(data => {
  
                data ? this.navigateToUseCaseDefinition(data[0], this.modelName) : ''
              })
            );
          openFileMappingTemplateAfterClosed.subscribe();
        } else {
  
          this.navigateToUseCaseDefinition(filesData.CorrelationId, this.modelName);
        }
      }
    }
  
    openFeatureMappingPopup(existingColumns, correlationId) {
      let timeseriesModel = false;
      this.columnMappingCorrelationId = correlationId;
      existingColumns = existingColumns.clear();
      existingColumns = this.inputColumns;
      existingColumns.add(this.timeSeriesColumns);
      //  existingColumns = this.timeSeriesColumns;// this.columnData;
      // existingColumns = existingColumns.add(this.targetColumn);
      // existingColumns = existingColumns.unshift(this.targetColumn);
      const featureMappingTemplate = this.dialogService.open(PstFeatureMappingComponent,
        {
          data: {
            featureColumnList: existingColumns, correlationIdForMappingCol: correlationId,
            existingTargetColumn: this.targetColumn, existingUniqueIdentifier: this.uniqueIdentifierVal
            , isTimeSeriesModel: this.isTimeSeriesOn
          }
        }).afterClosed.pipe(
          tap(objData => {
            if (objData) {
              this.isExistingTemplate = false;
              // this.ls.setLocalStorageData('modelCategory', '');
              this.adProblemStatementService.isMappedFromTemplate = 'True';
              this.displayUploadandDataSourceBlock = false;
              let changedTargetProperty = false;
              const mappedColumns = objData.mappingObj;
              //  
              const templateAvailableCol = Object.keys(mappedColumns);
              const templateColList = [];
  
              this.UniquenessDetails = objData.unique;
              for (const j in templateAvailableCol) {
                if (!this.coreUtilService.isNil(templateAvailableCol[j])) {
                  templateColList.push(templateAvailableCol[j]);
                }
              }
              const listOfMappedColumns = [];
              for (const i in mappedColumns) {
                if (!this.coreUtilService.isNil(mappedColumns[i])) {
                  listOfMappedColumns.push(mappedColumns[i]);
                }
              }
  
              if (mappedColumns.hasOwnProperty(this.targetColumn) || mappedColumns.hasOwnProperty(this.uniqueIdentifierVal)) {
                for (let mappedCol = 0; mappedCol <= listOfMappedColumns.length; mappedCol++) {
                  for (let templateCol = mappedCol; templateCol <= templateColList.length; templateCol++) {
                    if (this.targetColumn === templateColList[templateCol] && !changedTargetProperty) {
                      this.targetColumn = listOfMappedColumns[templateCol];
                      changedTargetProperty = true;
                    }
                    if (this.uniqueIdentifierVal === templateColList[templateCol]) {
                      this.uniqueIdentifierVal = listOfMappedColumns[templateCol];
                    }
                    if (this.timeSeriesColumns === templateColList[templateCol]) {
                      timeseriesModel = true;
                      this.isMappedTimeSeriesModel = true;
                      this.timeSeriesColumns = listOfMappedColumns[templateCol];
                      this.timeSeriesColPostMapping = this.timeSeriesColumns;
                      this.uniqueIdentifierVal = '';
                    }
                  }
                }
              }
              this.IfModelTemplateDataSource = objData.IfModelTemplateDataSource;
  
              this.isNextDisabled.emit(false);
              this.columnData = new Set(listOfMappedColumns);
              this.columnData.delete(this.targetColumn);
              this.columnData.delete(this.uniqueIdentifierVal);
              // console.log('timeseri', this.isTimeSeriesOn);
              if (this.isTimeSeriesOn) {
                this.columnData.delete(this.timeSeriesColumns);
                this.uniqueIdentifierDisabled = false;
                this.timeSeriesColumnsDisabled = true;
              }
              this.inputColumns.clear();
  
              // this.uniqueIdentifierVal = '';
  
              this.showSaveAs = true;
              this.showSave = true;
              this.showSelectAll = true;
              localStorage.setItem('featureMapping', 'True');
              this.correlationId = this.ls.getCorrelationId();
              // this.setClassOnElement('save', 'btn btn-primary');
              // this.setClassOnElement('saveAs', 'btn btn-primary');
              // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
            }
          })
        );
      featureMappingTemplate.subscribe();
    }
  
    openConfirmationDialog() {
      this.uts.usageTracking('Problem Statement', 'Data Source');
      if (this.pageSource === 'true') {
        this.pageSource = 'false';
      }
      const openDataSourceChangePopUp = this.dialogService.open(DatasourceChangeComponent, {}).afterClosed.pipe(
        //   tap(isDataFlushFlag => isDataFlushFlag ? this.openFileUploadWithDragDrop(this.correlationId) : '')
        tap(isDataFlushFlag => isDataFlushFlag ? this.getMetricAndEntityData(this.paramData.clientUID) : '')
      );
      openDataSourceChangePopUp.subscribe();
    }
  
  
  
    openFileUploadWithDragDrop(oldTemplateCorelationId) {
      this._modalService.show(ChangeDataSourceComponent,{ class: 'modal-dialog modal-xl', backdrop: 'static'}).content.outputData.subscribe(newFileDetails=>{
        if (typeof newFileDetails === 'boolean') {
          this.openModalForApiForSameModelName();
        } else {

          newFileDetails ? this.openFileProgressDialog(newFileDetails, this.modelName, this.correlationId) : '';
        }
      });
    }
  
    navigateToUseCaseDifination(existingCorelationId) {
      this.isExistingTemplate = true;
      this.isNewModel = false;
  
      this.getColumns();
      window.location.reload();
    }
  
    public ngOnDestroy() {
      /*  // this.setClassOnElement('anchorUseCase', 'ingrAI-sub-nav-button hide');
      this.eleRef.nativeElement.parentElement.parentElement.querySelectorAll('.ingrAI-sub-nav')[0]
        .lastElementChild.querySelectorAll('.btn-primary')[1].className = 'btn btn-primary bt-bg-color'; */
      // this.subscription.unsubscribe();
    }
  
    openRegressionPopup() {
      const openFileProgressAfterClosed = this.dialogService.open(RegressionPopupComponent,
        { data: {} }).afterClosed.pipe(
          tap(data => '')
        );
      openFileProgressAfterClosed.subscribe();
    }
    
    timeSeriesToggle() {
  
      if (this.customTargetColumn.has(this.targetColumn)) { this.targetColumn = ''; this.customTargetColumn.clear() }
      if (this.timeSeriesColumns !== '') {
        this.columnData.add(this.timeSeriesColumns);
        this.timeSeriesColumns = '';
      }
      if (this.inputColumns.size > 0) {
        this.inputColumns.forEach(column => {
          this.columnData.add(column);
        });
        this.inputColumns.clear();
      }
      if (!this.isTimeSeriesOn) {
        this.isDirty = 'Yes';
        this.hideTargetValForNone = true;
      }
      if (this.isTimeSeriesOn) {
        this.isDirty = 'Yes';
        this.hideTargetValForNone = false;
        this.showSelectAll = false;
        //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
        if (this.targetColumn !== '') {
          const dataType = this.checkTargetColumnDataType(this.targetColumn);
          if (dataType) {
            this.dataTypeVal(this.targetColumn);
            this.targetAggDisable = false;
            if (this.instaMLModel === true) {
              this.targetAggDisable = true;
            }
          } else {
            this.columnData.add(this.targetColumn);
            this.targetColumn = '';
            this.targetAggDisable = true;
            this.ns.error('Target Column should  be of Numeric datatype');
          }
          if (this.displayUploadandDataSourceBlock) {
            if (this.timeSeriesColPostMapping !== '') {
              this.timeSeriesColumns = this.timeSeriesColPostMapping;
              this.columnData.delete(this.timeSeriesColumns);
            }
          }
        }
      } else {
        if (this.targetColumn !== '') {
          this.showSelectAll = true;
          //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary');
          if (this.instaMLModel === true) {
            this.showSelectAll = false;
            //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
          }
        }
      }
    }
  
    checkTimeSeriesStatus() {
      if (this.isTimeSeriesOn) {
        this.hideTargetValForNone = false;
      }
    }
  
    onChangeOfSelect(modelVal, index) {
      // if (this.instaMLModel) {
      //   this.timeSeriesCheckboxValues[index].Steps = this.dataFrequency[index] ? this.dataFrequency[index]['Steps'] : null;
      // } else {
      //   if (modelVal.Steps === undefined) {
      //     this.timeSeriesCheckboxValues[index].Steps = null;
      //   } else {
      //     this.timeSeriesCheckboxValues[index].Steps = modelVal.Steps;
      //   }
      // }
    }
  
    openModalForApi() {
      if (this.coreUtilService.isNil(this.paramData.clientUID) || this.coreUtilService.isNil(this.paramData.deliveryConstructUId)) {
        this.ns.success('Kindly select a client or delivery structures');
      } else {
        // console.log('category--',this.selectedTemplate,'---',this.modelName);
        this.openModalDialog();
      }
  
    }
  
    openModalDialog() {
      if (this.filteredMetricData === undefined) {
        this.filteredMetricData = this._storeService.getMatircsFilteredData();
      }
      if (this.entityData === undefined) {
        this.entityData = this._storeService.getEntityData();
      }
      
      this._modalService.show(ChangeDataSourceComponent,{ class: 'modal-dialog modal-xl', backdrop: 'static'}).content.outputData.subscribe(filesData=>{
        this.openModalNameDialog(filesData);
      });
    }
  
    openModalForApiForSameModelName() {
      if (this.coreUtilService.isNil(this.paramData.clientUID) || this.coreUtilService.isNil(this.paramData.deliveryConstructUId)) {
        this.ns.success('Kindly select a client or delivery structures');
      } else {
        this.openModalDialogForSameModelName();
      }
  
    }
  
    openModalDialogForSameModelName() {
      this._modalService.show(ChangeDataSourceComponent,{ class: 'modal-dialog modal-xl', backdrop: 'static'}).content.outputData.subscribe(filesData=>{
        this.openFileProgDialog(filesData, this.modelName);
      });
    }
  
    openModalNameDialog(filesData) {
      this.Myfiles = filesData;
  
      const openTemplateAfterClosed =
        this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name' } }).afterClosed.pipe(
          tap(modelName => modelName ? this.openFileProgDialog(filesData, modelName) : '')
        );
  
      openTemplateAfterClosed.subscribe();
    }
  
    openFileProgDialog(filesData, modelName) {
      this.modelName = modelName;
      this.currentModelName = modelName;
      this.currentModel.emit(modelName);
      const totalSourceCount = filesData[0].sourceTotalLength;
      if (this.FMscenario === false) {
        if (totalSourceCount > 1) {
          const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: modelName, correlationId: '' } }).afterClosed
            .pipe(
              tap(data => data ? this.openModalMultipleDialog(data.body, modelName, filesData, data.dataForMapping, '', true) : '')
            );
          openDataSourceFileProgressAfterClosed.subscribe();
        } else {
          const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
            { data: { filesData: filesData, modelName: modelName, correlationId: '' } }).afterClosed
            .pipe(
              tap(data => data ? this.openFeatureMappingPopup(this.columnData, data.body[0]) : '')
            );
          openDataSourceFileProgressAfterClosed.subscribe();
        }
      } else {
        const openDataSourceFileProgressAfterClosed = this.dialogService.open(UploadProgressComponent,
          { data: { filesData: filesData, modelName: modelName, correlationId: '' } }).afterClosed
          .pipe(
            tap(data => data ? this.FMScenarioModelTrainingPopup() : '')
          );
        openDataSourceFileProgressAfterClosed.subscribe();
      }
    }
  
    navigateToUseCaseDefinition(correlationId, modelname?) {
  
      this.correlationId = correlationId;
      if (localStorage.getItem('featureMapping') === 'True') {
        this.uniqueIdentifierDisabled = true;
      }
      localStorage.setItem('featureMapping', 'False');
  
      this.ls.setLocalStorageData('correlationId', correlationId);
      // this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
      //   {
      //     queryParams: {
      //       'modelCategory': '',
      //       'displayUploadandDataSourceBlock': false,
      //       'correlationId': this.correlationId,
      //       'modelName': modelname,
      //       'pageSource': true
      //     }
      //   });
      // window.location.reload();
      const url = '/anomaly-detection/problemstatement/usecasedefinition?modelCategory=&displayUploadandDataSourceBlock=false&modelName=' + modelname;
      this.router.navigateByUrl(url);
      window.location.reload();
    }
    onTargetAggValSelection() {
      if (!this.coreUtilService.isNil(this.targetAggVal) && this.targetAggVal === 'None') {
        if (this.onload !== 1) {
          /*
          this.timeSeriesCheckboxValues = {
            '0': {
              'Name': 'Hourly',
              'Steps': null
            },
            '1': {
              'Name': 'Daily',
              'Steps': null
            },
            '2': {
              'Name': 'Weekly',
              'Steps': null
            },
            '3': {
              'Name': 'Monthly',
              'Steps': null
            },
            '4': {
              'Name': 'Fortnightly',
              'Steps': null
            },
            '5': {
              'Name': 'Quarterly',
              'Steps': null
            },
            '6': {
              'Name': 'Half-Year',
              'Steps': null
            },
            '7': {
              'Name': 'Yearly',
              'Steps': null
            },
            '8': {
              'Name': 'CustomDays',
              'Steps': null,
              'value': null
            }
          };
          */
          this.ns.warning('Kindly verify if you have selected the proper frequency ' +
            'for the input data which you have uploaded and then proceed to save');
        }
      }
      this.onload++;
    }
  
    disableAllInputs() {
      this.instaMLModel = true;
      this.targetColButtonDisabled = true;
      this.uniqueIdentifierDisabled = true;
      this.timeSeriesColumnsDisabled = true;
      this.targetAggDisable = true;
      this.inputColumnsDisabled = true;
      this.showSave = false;
      this.showSelectAll = false;
      //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
      //  // this.setClassOnElement('btnDeSelectAll', 'btn btn-primary btnselectall');
    }
  
    getMetricAndEntityData(clientUId) {
      this.getMetricsData(clientUId); // commented for Bug #887508
      // this.getDynamicEntity(clientUId);
    }
  
    getMetricsData(clientUId) {
      if (((this.env !== 'PAM' && this.env !== 'FDS') && (this.requestType !== 'AM' && this.requestType !== 'IO')) && this.fromApp !== 'FDS') {
        // this.appUtilsService.loadingStarted();
        // this.ps.getMetricsData(clientUId, this.paramData.deliveryConstructUId, this.userid).subscribe(
        //   data => {
        //     this.appUtilsService.loadingEnded();
        //     this.metricData = data;
        //     this.filterMetricData();
        //   },
        //   error => {
        //     this.appUtilsService.loadingEnded();
        //     this.openFileUploadWithDragDrop(this.correlationId);
        //     if (sessionStorage.getItem('Environment') !== 'FDS' && sessionStorage.getItem('RequestType') !== 'AM' && sessionStorage.getItem('RequestType') !== 'IO') {
        //       this.ns.error('Pheonix metrics api has no data.');
        //     }
        //   });
        this.openFileUploadWithDragDrop(this.correlationId);
      } else {
        this.openFileUploadWithDragDrop(this.correlationId);
      }
    }
  
    // filterMetricData() {
    //   if (this.metricData) {
    //     if (this.deliveryTypeName === 'release Management' || this.deliveryTypeName === 'AD') {
    //       this.deliveryTypeName = 'AD';
    //     } else if (this.deliveryTypeName === 'Agile Delivery' || this.deliveryTypeName === 'Agile') {
    //       this.deliveryTypeName = 'Agile';
    //     } else if (this.deliveryTypeName === 'Devops') {
    //       this.deliveryTypeName = 'Devops';
    //     } else {
    //       this.deliveryTypeName = this.deliveryTypeName;
    //     }
    //     if (this.metricData.length > 0) {
    //       let deliveryType = this.deliveryTypeName;
    //       if (this.deliveryTypeName === 'Devops') {
    //         deliveryType = 'Global';
    //       }
    //       this.filteredMetricData = this.metricData[0].MetricResultListData.filter(x => x.DeliveryTypeName === deliveryType);
    //     }
    //   }
    //   this.openFileUploadWithDragDrop(this.correlationId);
    // }
  
  
    // Bug raised by North American Team 
    // Desc :- Receive errors while fixing problem statement,Problem statement does not allow special characters
    // Fix :- Added \.\n \_ ( ) in regex, will allow user to add .
    isValidate(value) {
      value = value.trim();
      this.problemStatement = value; // Check for spaces
      if (value && value.length > 0) {
        const regex = /^[A-Za-z0-9\.\n\_() ]+$/;
        const isValid = regex.test(value);
        if (!isValid) {
          this.ns.warning('No special characters allowed.');
        } else {
          return true;
        }
      } else {
        return false;
      }
    }
  
    getDynamicEntity(clientUId) {
      this.appUtilsService.loadingStarted();
      this.adProblemStatementService.getDynamicEntity(clientUId, this.paramData.deliveryConstructUId, this.userid).subscribe(
        data => {
          this.appUtilsService.loadingEnded();
          this.entityData = data;
          this.getMetricsData(clientUId); // fix for Bug #887508
          // console.log(this.entityData);
        },
        error => {
          this.appUtilsService.loadingEnded(); this.getMetricsData(clientUId); // fix for Bug #887508
          this.ns.error('Pheonix metrics api has no data.');
        });
    }
  
    /**
    * Custom Target name pop up
    */
    public openCustomTargetNameDialog(): void {
      const openCustomTemplateAfterClosed =
        this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter target name', pageInfo: 'CreateCustomTarget' } }).afterClosed.pipe(
          tap(targetName => targetName ? this.isTargetName(targetName) : '')
        );
      openCustomTemplateAfterClosed.subscribe();
    }
  
    /**
     * Function to check target name exists
     * @param name - the name provided in pop up
     */
    private isTargetName(name: string): void {
  
      if (this.columnData.has(name) || this.inputColumns.has(name) || this.targetColumn === name || this.uniqueIdentifierVal === name) {
        this.ns.error('Column name exists');
      } else {
        if (this.targetColumn === 'CustomTarget') {
          this.targetColumn = name;
          // this.isCustomAttrClicked = true;
        }
        if (this.customTargetColumn.has(this.targetColumn)) { this.targetColumn = '' };
        this.customTargetColumn.clear();
        this.customTargetColumn.add(name);
        this.showCustomTarget = true;
        this.isCustomAttrClicked = false;
      }
    }
  
    /**
     * Function to remove the custom column from available list
     */
    public removeCustomColumn(): void {
      this.showCustomTarget = false;
      // this.customTargetColumn.delete(name);
    }
  
    /**
     * Function to chekc the target column is custom 
     * @param targetName passed to check
     * @param columnList passed to check with targetName
     */
    public checkIfCustomTargetColumn(targetName: string, columnList: Array<string>): void {
  
      const index = columnList.findIndex(data => data === targetName);
      if (index < 0) {
        this.customTargetColumn.clear();
        this.customTargetColumn.add(targetName);
        // this.showCustomTarget = true;
      }
    }
  
    customTargetToggle() {
      this.isCustomAttrClicked = !this.isCustomAttrClicked;
    }
  
    accessDenied() {
      this.ns.error('Access Denied');
    }
  
    next() {
      if (localStorage.getItem('targetColumn') === 'null') {
        this.save(true);
      } else {
        if (this.isDirty === 'No') {
          if (this.isFMModel) {
            if (this.inputColumns.size > 0) {
              this.redirectToNext();
              this.appUtilsService.loadingStarted();
            } else {
              this.save(true);
            }
          } else {
            this.redirectToNext();
            this.appUtilsService.loadingStarted();
          }
        } else {
          this.save(true);
        }
      }
    }
  
    redirectToNext() {
      if (this.coreUtilService.isNil(localStorage.getItem('isModelSelected'))
        && (this.router.url.indexOf('/anomaly-detection/problemstatement/templates') > -1)) {
        this.ns.warning('Please select a model in my models tab to proceed with its training and deployment.');
      }else if(!this.isDataValid(false)){
        this.ns.error('Target Column should  be of Numeric datatype');
      }else if((this.adProblemStatementService.getColumnsdata.IsModelTrained == true) && (this.isDirty == 'Yes')){
        this.ns.warning('Changes found please save before Proceeding to next');
      }else if (localStorage.getItem('targetColumn') === 'null' || !this.isTimeSeriesOn) {
        // this.notificationService.warning('Please fill the details and save to proceed with its training and deployment.');
        if (this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
          this.ns.warning('Fill the details and save before proceeding to the next page.');
        } else if (!this.coreUtilService.isNil(this.uniqueIdentifierVal)) {
          this.coreUtilService.disableADTabs(this.currentIndex, this.breadcrumbIndex, null, true);
          this.customRouter.redirectToNext();
        } else {
          this.ns.warning('Fill the details and save before proceeding to the next page.');
        }
      } else {
        this.coreUtilService.disableADTabs(this.currentIndex, this.breadcrumbIndex, null, true);
        this.customRouter.redirectToNext();
      }
    }
  
    previous() {
      if (this.router.url.indexOf('/anomaly-detection/problemstatement/usecasedefinition') > -1) {
        this.ls.setLocalStorageData('correlationId', '');
        this.ls.setLocalStorageData('modelName', '');
        this.ls.setLocalStorageData('modelCategory', '');
      }
      this.coreUtilService.disableADTabs(this.currentIndex, this.breadcrumbIndex, true, null);
      let url =  '/anomaly-detection/problemstatement/usecasedefinition';
      this.router.navigateByUrl(url);
      // this.customRouter.redirectToPrevious();
    }
  
    getModelsForCascading(customCascadeModelPopup) {
      //  if (this.isModelSaved === true) {
      const userId = sessionStorage.getItem('userId');
      this.appUtilsService.loadingStarted();
      this.cascadeService.getCustomCascadeModels(this.paramData.clientUID, this.paramData.deliveryConstructUId,
        userId, this.deliveryTypeName).subscribe(data => {
          this.appUtilsService.loadingEnded();
          if (data.Models.length > 0) {
            this.customModelNames = data.Models;
            if (!this.coreUtilService.isNil(this.customModelNames)) {
              if (this.customModelNames.length > 0) {
                let filteredModels = this.customModelNames.filter(model => model.CorrelationId !== this.correlationId);
                this.customModelNames = filteredModels;
                this.modalRef = this._modalService.show(customCascadeModelPopup, this.config);
              }
            }
          } else {
            this.ns.error('There are no deployed models with cascading option ON.');
          }
        },
          error => {
            this.appUtilsService.loadingEnded();
            this.ns.error(error.error);
          });
      /* } else {
        this.ns.error('Kindly save the model before proceeding.')
      } */
    }
  
    uniqueIdentifierMapping(cascadeMappingPopup, sourceCorid, cascadeId) {
      this.modalRef.hide();
      this.appUtilsService.loadingStarted();
      let targetCorid = this.ls.getCorrelationId();
      let uniqueIdName = this.uniqueIdentifierVal;
      let uniqueDataType = this.columnDataTypes[uniqueIdName];
      let targetColumn = this.targetColumn;
      this.cascadeService.getCascadeIdDetails(sourceCorid, targetCorid, cascadeId, uniqueIdName, uniqueDataType, targetColumn).subscribe(data => {
        this.appUtilsService.loadingEnded();
        this.mapData = data;
        if (data.CascadeModel !== null) {
          this.cascadeModelName = data.CascadeModel.cascadeModelName;
        }
        if (data.MappingData !== null) {
          this.mappingData = data.MappingData;
          this.modifyData(this.mappingData);
          this.newConfig = {
            backdrop: true,
            ignoreBackdropClick: true,
            class: 'deploymodle-confirmpopup',
            source: '',
            target: '',
            cascadedId: cascadeId,
            cascadeModelName: data.ModelName,
            AppId: this.mappingData[0][0].ApplicationID
          };
          this.modalRef = this._modalService.show(cascadeMappingPopup, this.newConfig);
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        this.ns.error(error.error);
      });
    }
  
    modifyData(rawModels, forMapping?) {
      const dataArray = [];
      const allAttributeNamesArray = [];
      const modelNamesArray = [];
      let id = 0;
      if (rawModels) {
        Object.entries(rawModels).forEach(
          ([key, value], i) => {
            const attributeArray = [];
            const attributeNames = [];
            if (forMapping == true) {
              this.svgWidth = Object.entries(rawModels).length * 260;
              if (i < (Object.entries(rawModels).length - 1)) {
                let previousLength;
                if (i === 0) {
                  previousLength = 0;
                } else {
                  previousLength = this.targetIdArray[i - 1];
                }
                this.targetIdArray[i] = previousLength + value['InputClumns'].length;
                this.uniqueIdArray[i][0] = 1;
                this.uniqueIdArray[i][1] = 1;
              }
              value['InputClumns'].forEach(element => {
                const modifiedAttribute = { id: id++, column: element, ModelName: value['ModelName'] };
                attributeArray.push(modifiedAttribute);
                attributeNames.push(element);
              });
            } else {
              if (value['InputColumns']) {
                Object.entries(value['InputColumns']).forEach(
                  ([key1, value1]) => {
                    let model = this.customModelNames.filter(el => el.CorrelationId === value['CorrelationId']);
                    let modifiedAttribute = {};
                    if (model.length > 0) {
                      modifiedAttribute = {
                        id: id++, column: key1, targetColumn: value['TargetColumn'], dataType: value1['Datatype'], uniqueValues: value1['UniqueValues'], metric: value1['Metric'], 'CorrelationId': value['CorrelationId'],
                        ModelName: value['ModelName'], ProblemType: value['ProblemType'], ModelType: value['ModelType'], ApplicationID: value['ApplicationID'], LinkedApps: value['LinkedApps'], Accuracy: model[0]['Accuracy']
                      };
                    } else {
                      modifiedAttribute = {
                        id: id++, column: key1, targetColumn: value['TargetColumn'], dataType: value1['Datatype'], uniqueValues: value1['UniqueValues'], metric: value1['Metric'], 'CorrelationId': value['CorrelationId'],
                        ModelName: value['ModelName'], ProblemType: value['ProblemType'], ModelType: value['ModelType'], ApplicationID: value['ApplicationID'], LinkedApps: value['LinkedApps'], Accuracy: 0
                      };
                    }
  
                    attributeArray.push(modifiedAttribute);
                    attributeNames.push(key1);
                  }
                );
              }
            }
            
            let modelName;
            if (i === 0) {
              modelName = { 'modelName': value['ModelName'], 'modelIndex': 'Start', 'correlationId': '', 'selectedTrainedModelName': '', targetColumn: '' };
            } else if (i === (Object.keys(rawModels).length - 1)) {
              modelName = { 'modelName': value['ModelName'], 'modelIndex': 'End', 'correlationId': '', 'selectedTrainedModelName': '', targetColumn: '' };
            } else {
              const modelIndex = 'Intermediate' + i;
              modelName = { 'modelName': value['ModelName'], 'modelIndex': modelIndex, 'correlationId': '', 'selectedTrainedModelName': '', targetColumn: '' };
            }
            dataArray.push(attributeArray);
            allAttributeNamesArray.push(attributeNames);
            if (forMapping) {
              const lengthOfArrays = allAttributeNamesArray.map(a => a.length);
              const max = lengthOfArrays.reduce(function (a, b) {
                return Math.max(a, b);
              });
              if (max >= 10 && max <= 12) {
                this.svgHeight = 700;
              }
              if (max > 12) {
                this.svgHeight = 800;
              }
            }
            modelNamesArray.push(modelName);
          }
        );
      }
      this.modelNames = modelNamesArray;
      if (forMapping) {
        this.dataForCascadeGraph = dataArray;
        this.allAttributes = allAttributeNamesArray;
      } else {
        this.mappingData = dataArray;
      }
  
    }
  
    openCascadeNamePopup(sourceUniqueId, targetUniqueId, cascadeModelNamePopup) {
      this.disableCascadeSave = false;
      this.newConfig = {
        backdrop: true,
        ignoreBackdropClick: true,
        class: 'deploymodle-confirmpopup',
        source: sourceUniqueId,
        target: targetUniqueId,
        cascadedId: '',
        cascadeModelName: '',
        AppId: ''
      };
      this.modalRef = this._modalService.show(cascadeModelNamePopup, this.newConfig);
    }
  
    confirmCustomCascade(sourceUniqueId, targetUniqueId, cascadeModelNamePopup, cascadedId, customCascadeModelName?, appId?) {
      this.modalRef.hide();
      const source = this.mappingData[0].filter(e => e.column === sourceUniqueId);
      const target = this.mappingData[1].filter(e => e.column === targetUniqueId);
      this.customCascadedId = cascadedId;
      const validation = this.validate(source[0], target[0]);
      
      if (validation === true) {
        if (this.coreUtilService.isNil(this.customCascadedId)) {
          this.openCascadeNamePopup(sourceUniqueId, targetUniqueId, cascadeModelNamePopup);
        } else {
          // this.saveCustomCascade(sourceUniqueId, targetUniqueId, customCascadeModelName);
        }
      } else {
        this.ns.error('Please map appropriate unique identifier. The records matching percentage is less than 70%.');
      }
    }
  
    // saveCustomCascade(sourceUniqueId, targetUniqueId, modelName?) {
    //   this.modalRef.hide();
    //   let source = this.mappingData[0].filter(e => e.column === sourceUniqueId);
    //   let target = this.mappingData[1].filter(e => e.column === targetUniqueId);
    //   this.cascadeModelName = modelName;
    //   this.customCascadeDataForSave = {
    //     'ModelName': '',
    //     'ClientUId': '',
    //     'CascadedId': null,
    //     'DeliveryConstructUID': '',
    //     'Category': '',
    //     'ModelList': {},
    //     'Mappings': {},
    //     'CreatedByUser': this.userid,
    //     'isModelUpdated': false,
    //     'UniqIdName': this.uniqueIdentifierVal,
    //     'UniqDatatype': this.columnDataTypes[this.uniqueIdentifierVal],
    //     'TargetColumn': this.targetColumn,
    //   };
    //   this.customCascadeDataForSave['ModelName'] = this.cascadeModelName;
    //   this.customCascadeDataForSave['ClientUId'] = this.mapData.ClientUid;
    //   this.customCascadeDataForSave['CascadedId'] = this.customCascadedId;
    //   this.customCascadeDataForSave['DeliveryConstructUID'] = this.mapData.DeliveryConstructUID;
    //   this.customCascadeDataForSave['Category'] = this.mapData.Category;
    //   if (this.mapData.CascadeModel !== null) {
    //     //  this.customCascadeDataForSave = this.mapData.CascadeModel;
    //     this.customCascadeDataForSave['Mappings'] = {};
    //     this.customCascadeDataForSave['ModelList'] = {};
    //     this.customCascadeDataForSave['ModelList'] = this.mapData.CascadeModel['ModelList'];
    //     let totalModels = Object.keys(this.mapData.CascadeModel['ModelList']).length;
    //     this.customCascadeDataForSave['ModelList']['Model' + (totalModels + 1)] = {
    //       'CorrelationId': this.ls.getCorrelationId(),
    //       'ModelName': this.modelName,
    //       'ProblemType': this.mappingData[1][0].ProblemType,
    //       'Accuracy': 0,
    //       'ModelType': this.mappingData[1][0].ModelType,
    //       'ApplicationID': this.mappingData[1][0].ApplicationID,
    //       'LinkedApps': this.mappingData[1][0].LinkedApps
    //     };
    //     let totalMappings = 0;
    //     if (this.mapData.CascadeModel['Mappings'] !== null) {
    //       totalMappings = Object.keys(this.mapData.CascadeModel['Mappings']).length;
    //       this.customCascadeDataForSave['Mappings'] = this.mapData.CascadeModel['Mappings'];
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)] = {};
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['UniqueMapping'] = {};
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['TargetMapping'] = {};
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['UniqueMapping']['Source'] = sourceUniqueId;
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['UniqueMapping']['Target'] = targetUniqueId;
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['TargetMapping']['Source'] = source[0].targetColumn; // this.mappingData[1][0].targetColumn;
    //       this.customCascadeDataForSave['Mappings']['Model' + (totalMappings + 1)]['TargetMapping']['Target'] = source[0].ModelName + '_' + source[0].targetColumn;
    //     } else {
    //       this.customCascadeDataForSave['Mappings'] = {
    //         'Model1': {
    //           'UniqueMapping': {
    //             'Source': sourceUniqueId,
    //             'Target': targetUniqueId
    //           },
    //           'TargetMapping': {
    //             'Source': source[0].targetColumn, // this.mappingData[1][0].targetColumn,
    //             'Target': source[0].ModelName + '_' + source[0].targetColumn
    //           }
    //         }
    //       }
    //     }
    //     console.log(this.customCascadeDataForSave);
    //   } else {
    //     this.customCascadeDataForSave['Mappings'] = {};
    //     this.customCascadeDataForSave['ModelList'] = {};
    //     this.customCascadeDataForSave['Mappings']['Model1'] = {};
    //     this.customCascadeDataForSave['Mappings']['Model1']['UniqueMapping'] = {};
    //     this.customCascadeDataForSave['Mappings']['Model1']['TargetMapping'] = {};
    //     this.customCascadeDataForSave['Mappings']['Model1']['UniqueMapping']['Source'] = sourceUniqueId;
    //     this.customCascadeDataForSave['Mappings']['Model1']['UniqueMapping']['Target'] = targetUniqueId;
    //     this.customCascadeDataForSave['Mappings']['Model1']['TargetMapping']['Source'] = source[0].targetColumn; // this.mappingData[1][0].targetColumn;
    //     this.customCascadeDataForSave['Mappings']['Model1']['TargetMapping']['Target'] = source[0].ModelName + '_' + source[0].targetColumn;
    //     this.customCascadeDataForSave['ModelList']['Model1'] = {
    //       'CorrelationId': this.mappingData[0][0].CorrelationId,
    //       'ModelName': this.mappingData[0][0].ModelName,
    //       'ProblemType': this.mappingData[0][0].ProblemType,
    //       'Accuracy': this.mappingData[0][0].Accuracy,
    //       'ModelType': this.mappingData[0][0].ModelType,
    //       'ApplicationID': this.mappingData[0][0].ApplicationID,
    //       'LinkedApps': this.mappingData[0][0].LinkedApps
    //     };
    //     this.customCascadeDataForSave['ModelList']['Model2'] = {
    //       'CorrelationId': this.mappingData[1][0].CorrelationId,
    //       'ModelName': this.mappingData[1][0].ModelName,
    //       'ProblemType': this.mappingData[1][0].ProblemType,
    //       'Accuracy': 0,
    //       'ModelType': this.mappingData[1][0].ModelType,
    //       'ApplicationID': this.mappingData[1][0].ApplicationID,
    //       'LinkedApps': this.mappingData[1][0].LinkedApps
    //     };
    //     console.log(this.customCascadeDataForSave);
    //   }
    //   this.appUtilsService.loadingStarted();
    //   this.cascadeService.saveCustomCascadeModels(this.customCascadeDataForSave).subscribe(data => {
    //     if (data.IsInserted === true && data.IsException === false) {
    //       this.appUtilsService.loadingEnded();
    //       this.ns.success('Data Processed Successfully.');
    //       this.customCascadedId = data.CascadedId;
    //       sessionStorage.setItem('customCascadedId', this.customCascadedId);
    //       this.isIncludedinCustomCascade = true;
    //       this.customCascadeDataForSave = {};
    //       if (this.FMscenario === true) {
    //         window.location.reload();
    //       } else {
    //         this.inputColumns.add(this.mappingData[0][0].ModelName + '_' + this.mappingData[0][0].targetColumn);
    //         if (this.mappingData[0][0].ProblemType === 'Classification' || this.mappingData[0][0].ProblemType === 'Multi_Class') {
    //           this.inputColumns.add(this.mappingData[0][0].ModelName + '_Proba1');
    //         }
    //         this.previousModelTarget = source[0].ModelName + '_' + source[0].targetColumn;
    //         this.proba1Value = source[0].ModelName + '_Proba1';
    //       }
    //     } else {
    //       this.appUtilsService.loadingEnded();
    //       this.customCascadeDataForSave = {};
    //       this.ns.error('Error in processing the data.');
    //     }
    //   },
    //     error => {
    //       this.appUtilsService.loadingEnded();
    //       this.customCascadeDataForSave = {};
    //       this.ns.error('Error in processing the data.');
    //     });
    // }
  
    validate(source, target) {
      let sourceStringArray = source.uniqueValues;
      let targetStringArray = target.uniqueValues;
      if (source.dataType.includes('float') || source.dataType.includes('Float') || source.dataType.includes('int') || source.dataType.includes('Integer')) {
        sourceStringArray = source.uniqueValues.map(String);
      }
      if (target.dataType.includes('float') || target.dataType.includes('Float') || target.dataType.includes('int') || target.dataType.includes('Integer')) {
        targetStringArray = target.uniqueValues.map(String);
      }
      /*  const isSubset = sourceStringArray.every(val => targetStringArray.includes(val));
      if (isSubset === true) {
        return true;
      } else {
        return false;
      } */
      const subSetResult = this.unionOfArrays(sourceStringArray, targetStringArray, true).length;
      const percentageMatch = (subSetResult / sourceStringArray.length) * 100;
      let matchingPercentage = this.envService.environment['categoricalMatchPercentage'];
      if (matchingPercentage === undefined) {
        matchingPercentage = 70;
      }
      if (percentageMatch >= matchingPercentage) {
        return true;
      } else {
        return false;
      }
    }
  
    // showCascadeMapping(cascadeMappingPopup) {
    //   if (!this.isFMModel) {
    //     if (this.isModelSaved === true) {
    //       this.appUtilsService.loadingStarted();
    //       this.cascadeService.getCustomCascadeDetails(this.customCascadedId).subscribe(data => {
    //         this.appUtilsService.loadingEnded();
    //         if (data.IsException === true) {
    //           this.ns.error(data['ErrorMessage']);
    //         } else if (!this.coreUtilService.isNil(data.ModelList)) {
    //           this.modifyData(data.ModelList, true);
    //           const cascadeConfig = {
    //             backdrop: true,
    //             ignoreBackdropClick: true,
    //             class: 'cascade-popup'
    //           };
    //           this.modalRef = this._modalService.show(cascadeMappingPopup, cascadeConfig);
    //         } else {
    //           this.ns.error('Error in processing the data.');
    //         }
    //       },
    //         error => {
    //           this.appUtilsService.loadingEnded();
    //           this.ns.error('Error in processing the data.');
    //         });
    //     } else {
    //       this.ns.error('Kindly save the model before proceeding.')
    //     }
    //   } else {
    //     const cascadeConfig = {
    //       backdrop: true,
    //       ignoreBackdropClick: true,
    //       class: 'cascade-popup'
    //     };
    //     this.modalRef = this._modalService.show(cascadeMappingPopup, cascadeConfig);
    //   }
    // }
  
  
    // showFrequency(event, frequencyType) {
    //   // if (event.checked) {
    //   //   this.isExpandTab[frequencyType] = true;
    //   // } else {
    //   //   this.isExpandTab[frequencyType] = false;
    //   // }
    //   // event.preventDefault();
    // }
  
    unionOfArrays(arr1, arr2, isUnion) {
      let result = [];
      for (let i = 0; i < arr1.length; i++) {
        let item1 = arr1[i],
          found = false;
        for (let j = 0; j < arr2.length && !found; j++) {
          found = item1 === arr2[j];
        }
        if (found === !!isUnion) { // isUnion is coerced to boolean
          result.push(item1);
        }
      }
      return result;
    }
  
    // onEnterModelName(sourceUniqueId, targetUniqueId, modalName) {
    //   this.disableCascadeSave = true;
    //   const valid = this.coreUtilService.isSpecialCharacter(modalName);
    //   if (valid === 0) {
    //     this.ns.error('Kindly enter a valid name.');
    //     this.disableCascadeSave = false;
    //   } else {
    //     if (modalName !== '') {
    //       let name = modalName;
    //       this.appUtilsService.loadingStarted();
    //       this.ps.getExistingModelName(modalName).subscribe(message => {
    //         if (message === false) {
    //           this.appUtilsService.loadingImmediateEnded();
    //           this.saveCustomCascade(sourceUniqueId, targetUniqueId, name);
    //         } else {
    //           message = 'The model name already exists. Choose a different name.';
    //           this.ns.error(message);
    //           this.disableCascadeSave = false;
    //           this.appUtilsService.loadingImmediateEnded();
    //         }
    //       }, error => {
    //         this.ns.error('Something went wrong.');
    //         this.appUtilsService.loadingImmediateEnded();
    //         this.disableCascadeSave = false;
    //       });
    //     } else {
    //       this.ns.error('Enter Model Name');
    //       this.disableCascadeSave = false;
    //     }
    //   }
    // }
  
    // callFMScenarioStatus() {
    //   this.ps.getFMModelsStatus(this.paramData.clientUID, this.paramData.deliveryConstructUId, this.userid).subscribe(data => {
    //     const modelsInProgress = data.filter(model => (model.Status === 'P' || model.Status === 'null'));
    //     this.completedModels = data.filter(model => model.Status === 'C');
    //     if (!this.coreUtilService.isNil(modelsInProgress)) {
    //       if (modelsInProgress.length > 0) {
    //         this.retry();
    //       } else {
    //         if (this.completedModels.length > 0) {
    //           const timeOffset = new Date().getTimezoneOffset() * 60000; // Converting offset time in milliseconds
    //           let models = this.completedModels.filter(model => (new Date().getTime() - (new Date(model.CreatedOn).getTime() - timeOffset)) < 600000); // 10 minutes difference from current time
    //           /* const lastElement = this.completedModels[(this.completedModels.length - 1)];
    //           const lastElTime = new Date(lastElement.CreatedOn).getTime();
    //           const localTime = new Date(lastElTime - timeOffset);*/
    //           if (models.length > 0) {
    //             this.completedModels = models;
    //             this.modalRef = this._modalService.show(this.FMScenarioPopup, this.config);
    //           }
    //         }
    //         if (this.FMSubscription !== undefined) {
    //           this.FMSubscription.unsubscribe();
    //         }
    //       }
    //     }
    //   }, error => {
    //     this.ns.success('Error');
    //   });
    // }
  
    // retry() {
    //   this.FMSubscription = timer(10000).subscribe(() =>
    //     this.callFMScenarioStatus());
    //   return this.FMSubscription;
    // }
  
    closeFMPopup() {
      this.modalRef.hide();
      if (this.router.url.includes('anomaly-detection/problemstatement/templates') === true) {
        window.location.reload();
      }
    }
  
    FMScenarioModelTrainingPopup() {
      this.modalRef = this._modalService.show(this.FMScenarioTrainingStatus, this.config);
    }
  
    redirectToMyModels() {
      this.modalRef.hide();
      if (this.FMscenario === true) {
        this.isNextDisabled.emit(true);
        // this.callFMScenarioStatus();
      }
      const url = '/anomaly-detection-dashboard';
      this.router.navigateByUrl(url);
    }
  
    changeProblemStatement() {
      this.isDirty = 'Yes';
      if (sessionStorage.getItem('isModelDeployed') === 'true') {
        this.isNextDisabled.emit(true);
      }
    }
  
  
    /**
     * Function to get the status of Data Clean Up process.
     */
    getDataCleanUpStatus() {
      this.appUtilsService.loadingStarted();
      const pageInfo = 'DataCleanUp';
      this.adProblemStatementService.GetStatusForDEAndDTProcess(this.correlationId, pageInfo, this.userid).subscribe(response => {
        if (!this.coreUtilService.isNil(response.status)) {
          if (response.status == ApiStatus.InProgress) {
            this.retryGetDataCleanUpStatus();
          } else if (response.status == ApiStatus.Completed) {
            this.appUtilsService.loadingImmediateEnded();
            this.ns.success('Data CleanUp completed successfully.');
            this.getDataPreprocessingStatus();
          } else if (response.status = ApiStatus.Error) {
            this.ns.error('Error ocuured during Data clean Up process.');
            this.appUtilsService.loadingEnded();
          }
        } else {
          this.ns.error('Error ocuured during Data clean Up process.');
          this.appUtilsService.loadingEnded();
        }
      });
    }
  
    retryGetDataCleanUpStatus() {
      this.timerSubscripton = timer(2000).subscribe(() => {
        this.getDataCleanUpStatus();
      });
      return this.timerSubscripton;
    }
    // Data Clean Up process ends.
  
    /**
     * Function to get the status of Data Pre-processing process.
     */
    getDataPreprocessingStatus() {
      this.appUtilsService.loadingStarted();
      const pageInfo = 'DataPreprocessing';
      this.adProblemStatementService.GetStatusForDEAndDTProcess(this.correlationId, pageInfo, this.userid).subscribe(response => {
        if (!this.coreUtilService.isNil(response.status)) {
          if (response.status == ApiStatus.InProgress) {
            this.retryGetDataPreprocessingStatus();
          } else if (response.status == ApiStatus.Completed) {
            this.appUtilsService.loadingEnded();
            this.ns.success('Data processing completed successfully.');
            this.redirectToNext();
          } else if (response.status = ApiStatus.Error) {
            this.ns.error('Error ocuured during Data Preprocessing.');
            this.appUtilsService.loadingEnded();
          }
        } else {
          this.ns.error('Error ocuured during Data Preprocessing.');
          this.appUtilsService.loadingEnded();
        }
      });
  
    }
  
    retryGetDataPreprocessingStatus() {
      this.timerSubscripton = timer(2000).subscribe(() => {
        this.getDataPreprocessingStatus();
      });
      return this.timerSubscripton;
    }
    //Data Pre-process Ends.
  compareObjects(o1: any, o2: any) {
    if (o1 == o2) { // it has to be double equals (==)
      return true;
    }
  }

  

  checkForTimeSeries() {
    if (this.isOnlyNumericType(this.targetColumn) == true && this.inputColumns.size == 1) {
      this.inputColumns.forEach(a => {
        if (this.columnDataTypes[a] == 'Date') {
          this.onSelectChange(a,"columnsContainer","timeSeriesColumnsContainer")
          this.isTimeSeriesOn = true;
          this.columnData[a] = 'Date';
          this.enalbleTimeseries();
        }
      });
    }
  }

  enalbleTimeseries(){
    if (this.isTimeSeriesOn) {
      this.isDirty = 'Yes';
      this.hideTargetValForNone = false;
      this.showSelectAll = false;
      //  // this.setClassOnElement('btnSelectAll', 'btn btn-primary btnselectall');
      if (this.targetColumn !== '') {
        const dataType = this.checkTargetColumnDataType(this.targetColumn);
        if (dataType) {
          this.dataTypeVal(this.targetColumn);
          this.targetAggDisable = false;
          if (this.instaMLModel === true) {
            this.targetAggDisable = true;
          }
        } else {
          this.columnData.add(this.targetColumn);
          this.targetColumn = '';
          this.targetAggDisable = true;
          this.ns.error('Target Column should  be of Numeric datatype');
        }
        if (this.displayUploadandDataSourceBlock) {
          if (this.timeSeriesColPostMapping !== '') {
            this.timeSeriesColumns = this.timeSeriesColPostMapping;
            this.columnData.delete(this.timeSeriesColumns);
          }
        }
      }
    }
  }

  isDataValid(msgRequired?){
    if(this.coreUtilService.isNil(this.latestTargetValue) || this.isOnlyNumericType(this.latestTargetValue)){
      return true;
    }else{
      if(msgRequired == true){
      this.ns.error('Target Column should  be of Numeric datatype.');
      }
      return false;
    }
  }

  }
