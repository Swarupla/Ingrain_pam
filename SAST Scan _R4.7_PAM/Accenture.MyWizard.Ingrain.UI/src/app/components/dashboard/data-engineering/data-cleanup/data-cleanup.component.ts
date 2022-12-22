import { Component, OnInit, ViewChild, DoCheck, AfterViewInit, AfterContentInit, ChangeDetectionStrategy, Output, ElementRef, TemplateRef, EventEmitter } from '@angular/core';
import { timer, Subscription, throwError, of } from 'rxjs';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { catchError, tap, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { EMPTY } from 'rxjs';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ThrowStmt } from '@angular/compiler';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';

@Component({
  selector: 'app-data-cleanup',
  templateUrl: './data-cleanup.component.html',
  styleUrls: ['./data-cleanup.component.scss']
})
export class DataCleanupComponent implements OnInit, DoCheck, AfterContentInit {

  @ViewChild('helppanelopen', { static: false }) helppanelopen;
  @ViewChild('helppanelclose', { static: false }) helppanelclose;
  @ViewChild('selectedDataType', { static: false }) selectedDataType;
  @ViewChild('confirmChangesOnDataCuration', { static: true }) confirmChangesOnDataCuration;
  @ViewChild('showpopover', { static: false }) showpopover;

  status = false;
  cleanupData = false;
  counter = 0;
  count = 0;
  allowedCount: number;
  dataToCleanUp;
  repeatedCalls;
  userId: string;
  correlationId = '';
  pageInfo = 'DataCleanUp';
  page = 1;
  itemsPerPage = 10;
  pythonProgress;
  pythonProgressStatus;
  modalRef: BsModalRef;
  config = {
    backdrop: true,
    class: 'deploymodle-confirmpopup'
  };

  processData;
  featureNames;
  featureNameArray = [];

  cleanupDataTypeArray;
  cleanupCorrelation;
  cleanupScale;
  objecttoarraypipe;
  timerSubscripton: Subscription;
  timerSubscriptonForSave: Subscription;
  dataEngineeringSubscription: Subscription;
  showhelppanel = false;
  totalRowsOfFeatureNames: number;
  sizeofScaleObject: number;
  sizeofDatatypesObject: number;
  fixMissingValue = new Set([]);
  fixDataQuality = new Set([]);
  correlation = [];
  cleanedupData: {};
  selectedDataTypesArray: any;
  postselectedDatatypeObject = {};
  featurenameinobject: any;
  defaultDataType;
  corelationForPrescription: Set<[]>;
  idDataTypeForPrescription = new Set([]);
  isDataTypeContainsId: boolean;
  sizeofDatatypesFromDB: number;
  isDataTypeDropDownEmpty: boolean;
  problemStatementData;
  correlationtoRemove: any;
  dataCleanUpObject: any;
  dataCleanUpData: any;
  featureNamesObject: any;
  fixedFeatureNamesObject = new Set([]);
  selectedDataTypesToSave = {};
  selectedcleanupScalesToSave = {};
  newCorrelationIdAfterCloned;
  formatdatetimefield: boolean;
  selectdefaultDateFormat: {} = {};
  selectvalue: any;
  payloaddata = {};
  // payloaddata = { "Datetimeformat": {
  //    "Ship Date": { 
  //      "type": "datetime64[ns]", 
  //      "format": "yyyy/mm/dd"
  //     },
  //     "Order Date": {
  //       "type": "datetime64[ns]", 
  //       "format": "dd/Mon/yyyy"
  //     }
  //  }};

  //    restrict = { "dataModification" : { "prepopulate": {

  //     "bmi": "True",
  //     "sex": "True",

  // }
  // }
  // };


  mappings = {
    featurename: 'Feature Name',
    datatype: 'Datatype',
    correlation: 'Correlation',
    scale: 'Scale',
    outlier: 'Outlier',
    imbalanced: 'ImBalanced',
    skewness: 'Skewness',
    dataqualityscore: 'Data_Quality_Score',
    missingvalues: 'Missing Values',
    q_info: 'Q_Info',
    correlationtoremove: 'CorrelationToRemove'
  };



  isLoading = true;
  tempFeaturesNamestobeFixed = [];
  featuresNamestobeFixed = new Set([]);
  featuresNamesLessDataQuality = new Set();
  datatypeForPrescriptionEmpty = new Set([]);
  defaultDataTypes = ['category', 'Text', 'float64', 'int64', 'ID', 'datatime64[ns]'];
  // defaultDataTypes = ['Not Applicable']
  // defaultScales = ['Nominal', 'Ordinal'];
  defaultScales = ['Not Applicable'];
  fixEmptyDatatypeFeatureNames = new Set([]);
  pstModelName: any;
  pstDataSource: any;
  newModelName: {};
  noData: string;
  dataQualityInfo = '';
  pstUseCase: any;
  searchText;
  prescritiveFlag: Boolean = false;
  valueexit: Boolean = false; // not used in html 
  // @Input() searchText = '';
  prescriptionCorrelatedVal;
  highlyCorrelatedKeys = [];
  DtypeModifiedColumns = {};
  DateFormatColumns = {};
  ScaleModifiedColumns = {};
  saveSubscription: Subscription;
  nextDisabled: Boolean = true;
  fixitBtnDisabled: Boolean = false;
  countOfUnselectedDataType = 0;
  featurenamecount = 0;
  unselectedDatatypeFeatures = new Set();
  finalcount = 0;
  redDatatype = false;
  subscription: Subscription;
  // paramData: any;
  showCalc: boolean;
  topValueForTooltip: number;
  targetColumn;
  uniqueIdentifer;
  shouldSave = false;
  dataTypeChange = '';
  isDirty = '';
  deliveryTypeName;
  ProcessedRecords: any;
  DateFormat: any;
  public modelType: string;
  private modalRefForManual: BsModalRef | null;
  private addfeatureconfig = {
    ignoreBackdropClick: true,
    class: 'modal-dialog-centered modal-dialog-scrollable modal-lg preprocess-new-addfeature-manualmodal'
  };
  public featuresAddedbyUser;
  public featureDataTypes;
  public columnList;
  public columnListNoText;
  public IsCustomColumnSelected;
  public Existing_Features_Disabled = new Set();
  public retainedColumn;
  readOnly;
  IsModelTrained = false;
  currentIndex = 1;
  breadcrumbIndex = 0;

  // isNextDisabled = false;
  modalRefDataQuality: BsModalRef | null;
  featureNameDataQualityLow = ''; // Not in use
  message = '';
  public innerHeight;
  attributesWithId: string = '';
  disabledDateTimeForTimeSeriesModel = {};
  modelCategory : string;
  decimalPoint;

  @Output() isNextDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() onDataSave : EventEmitter<boolean> = new EventEmitter<boolean>();

  constructor(private dataEngineering: DataEngineeringService, private eleRef: ElementRef,
    private ns: NotificationService, private ls: LocalStorageService, private coreUtilsService: CoreUtilsService,
    private dialogService: DialogService, private router: Router,
    private aus: AppUtilsService, private ps: ProblemStatementService, private uts: UsageTrackingService,
    private envService : EnvironmentService,private msalAuthentication: AuthenticationService,
    private _modalService: BsModalService, private customRouter: CustomRoutingService) {
      this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
  }

  ngOnInit() {
    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      if (this.router.url.toString().indexOf('fromApp=vds') > -1) {
        this.msalAuthentication.getToken().subscribe(data => {
          if (data) {
            this.envService.setMsalToken(data);
          }
        });
      }
    }
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.DateFormat = [
      'yyyy-mm-dd HH:MM:SS',
      'yyyy-mm-dd',
      'yyyy/mm/dd',
      'mm-dd-yyyy',
      'mm/dd/yyyy',
      'dd-mm-yyyy',
      'dd/mm/yyyy',
      'dd-Mon-yyyy',
      'dd/Mon/yyyy',
      'mm-dd-yy',
      'mm/dd/yy',
      'dd-mm-yy',
      'dd/mm/yy'
    ];
    this.formatdatetimefield = false;
    if (this.router.url === '/dashboard/dataengineering/dataclaanup') {
      this.router.navigate(['/dashboard/dataengineering/datacleanup'],
        {
          queryParams: {}
        });
      // this.ngOnInit();
    }
    this.ls.setLocalStorageData('lessdataquality', '');
    this.correlationId = this.ls.getCorrelationId();
    this.userId = this.aus.getCookies().UserId;
    this.uts.usageTracking('Data Engineering', 'Data Curation');
    this.disableTabs();
    this.getDataCleanUpData();
    this.modelCategory = this.ls.getModelCategory();
  }

  ngDoCheck() {
    this.dataEngineering.setApplyFlag(this.nextDisabled);
    if (!this.isLoading) {
      this.isNextEnabled();
    }

    // this.checkForUnselectedDatatype(this.featureNames);
  }

  showSearchedResult(value: string) {
    // console.log('---------', value);
    this.searchText = value;
  }

  prescriptionOpen(event) {
    event.preventDefault();
    this.status = !this.status;
  }

  getDataCleanUpData(): any {
    this.dataEngineeringSubscription = this.dataEngineering
      .getDataCleanUp(this.correlationId, this.userId, this.pageInfo)
      .pipe(
        switchMap(
          data => {
            if (this.IsJsonString(data)) {
              const parsedData = JSON.parse(data);
              if (parsedData.hasOwnProperty('message')) {
                return this.dataEngineering.getDataCleanUp(this.correlationId, this.userId, this.pageInfo);
              } else if (parsedData.hasOwnProperty('useCaseDetails')) {
                return of(parsedData);
              } else {
                this.noData = 'Format from server is not Recognised';
                this.ns.error(`Error occurred: Due
                 to some backend data process
                  the relevant data could not be produced.
                   Please try again while we troubleshoot the error`);
                this.unsubscribe();
                return EMPTY;
              }
            } else if (data.constructor === Object) {
              this.setAttributes(data);
              return EMPTY;
            } else if (data.constructor === String) {
              this.noData = data.toString();
              this.ns.success(data);
              this.unsubscribe();
              return EMPTY;

            } else {
              this.noData = 'Format from server is not Recognised';
              this.ns.error(`Error occurred:
               Due to some backend data process
                the relevant data could not be produced.
                 Please try again while we troubleshoot the error`);
              this.unsubscribe();
            }
          }
        ), catchError(error => {
          return throwError(error);
        })).subscribe(data => {
          this.setAttributes(data);
        });
  }

  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
    if (!this.coreUtilsService.isNil(this.timerSubscriptonForSave)) {
      this.timerSubscriptonForSave.unsubscribe();
    }
    if (!this.coreUtilsService.isNil(this.saveSubscription)) {
      this.saveSubscription.unsubscribe();
    }
    if (!this.coreUtilsService.isNil(this.dataEngineeringSubscription)) {
      this.dataEngineeringSubscription.unsubscribe();
    }
    this.isLoading = false;
  }

  IsJsonString(str) {
    try {
      JSON.parse(str);
    } catch (e) {
      return false;
    }
    return true;
  }

  setAttributes(data) {

    // this.selectdefaultDateFormat = this.payloaddata['Datetimeformat'];
    // this.selectvalue = this.selectdefaultDateFormat['Ship Date']['format'];
    this.isNextDisabled.emit(true);
    this.dataCleanUpData = data;
    this.disableTabs();
    this.pstDataSource = data.DataSource;
    this.pstModelName = data.ModelName;
    this.setVariableRequiredForAddFeatureComponent(data); // For Add feature
    this.targetColumn = data.TargetColumn;
    this.uniqueIdentifer = data.TargetUniqueIdentifier;
    this.deliveryTypeName = data.Category;
    if (!this.coreUtilsService.isNil(data.IsModelTrained)) {
      this.IsModelTrained = data.IsModelTrained;
    }
    if (!this.coreUtilsService.isNil(localStorage.getItem('fromApp'))) {
      this.ls.setLocalStorageData('modelName', this.pstModelName);
    }
    this.pstUseCase = data.BusinessProblem;
    this.processData = this.dataCleanUpData.processData;
    const useCaseList = this.coreUtilsService.isNil(this.dataCleanUpData.useCaseDetails)
      ? this.dataCleanUpData.useCaseDetails : JSON.parse(this.dataCleanUpData.useCaseDetails);

    if (!this.coreUtilsService.isNil(useCaseList)) {
      this.pythonProgressStatus = this.coreUtilsService.isNil(useCaseList.Status) ? '' : useCaseList.Status;
      this.pythonProgress = this.coreUtilsService.isNil(useCaseList.Progress) ? '' : useCaseList.Progress;
      if (!this.coreUtilsService.isNil(this.pythonProgressStatus) && this.pythonProgressStatus === 'P') {
        this.isLoading = true;
        this.isNextDisabled.emit(this.isLoading);
        this.pythonProgress = useCaseList.Progress;
        this.retry();
      } else if (!this.coreUtilsService.isNil(this.pythonProgressStatus) && this.pythonProgressStatus === 'E') {
        this.isLoading = false;
        this.isNextDisabled.emit(this.isLoading);
        this.ns.error('Error occurred: Please check with system administration');
      } else if (!this.coreUtilsService.isNil(this.pythonProgressStatus) && this.pythonProgressStatus === 'C') {
        this.isLoading = false;
        this.isNextDisabled.emit(this.isLoading);
        this.processData = JSON.parse(this.dataCleanUpData.processData);
        // old line - this.featureNamesObject = this.processData[this.mappings.featurename];
        // Attribute names should come in sorted
        this.featureNamesObject = this.processData[this.mappings.featurename];
        this.prepopulateFeatures();
        this.sortAttributeList();
        this.correlationtoRemove = this.processData.CorrelationToRemove;
        this.ProcessedRecords = this.processData.ProcessedRecords;
        if (this.ProcessedRecords !== undefined && this.ProcessedRecords !== null) {
          this.ns.success(this.ProcessedRecords);
        }
        if (Object.keys(this.correlationtoRemove).length !== 0) {

          this.getHighlyCorrelatedVlues(this.correlationtoRemove);
        }
        this.unsubscribe();
      } else {
        this.noData = 'Format from server is not Recognised';
        let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
        msg = msg + ' Please try again while we troubleshoot the error';
        this.ns.error(msg);
        this.unsubscribe();
      }
    } else {
      const parsedData = JSON.parse(data);
      if (parsedData.hasOwnProperty('message') && !this.pythonProgressStatus) {
        this.getDataCleanUpData();
        return;
      }
    }

    if (!(this.fixDataQuality.size > 0 || this.fixMissingValue.size > 0) &&
      !(this.unselectedDatatypeFeatures.size > 0 || this.fixEmptyDatatypeFeatureNames.size > 0
        || this.isEmptyObject(this.correlationtoRemove))) {
      this.status = true;
    }
  }

  setAttributesafterSave(data) {
    this.isNextDisabled.emit(true);
    this.disableTabs();
    this.dataCleanUpData = data;
    this.pstDataSource = data.DataSource;
    this.pstModelName = data.ModelName;
    this.setVariableRequiredForAddFeatureComponent(data); // For Add feature
    this.pstUseCase = data.BusinessProblem;
    this.processData = this.dataCleanUpData.processData;
    // var useCaseList = JSON.parse(this.dataCleanUpData.useCaseDetails);

    this.pythonProgressStatus = this.dataCleanUpData.Status;
    this.pythonProgress = this.dataCleanUpData.Progress;
    if (this.pythonProgressStatus === 'P') {
      if (this.modelCategory.toString().trim() === "Agile") {
        this.onDataSave.emit(true);
      }
      this.isNextDisabled.emit(true);
      this.isLoading = true;
      this.pythonProgress = this.dataCleanUpData.Progress;
      this.retrySave();
    } else if (this.pythonProgressStatus === 'E') {
      this.isNextDisabled.emit(false);
      this.isLoading = false;
      this.ns.error('Error occurred: Please check with system administration');
    } else if (this.pythonProgressStatus === 'C') {
      this.isNextDisabled.emit(false);
      this.isLoading = false;
      this.ns.success('Data saved successfully.');
      this.dataTypeChange = 'UpdateDataCleanUpSuccess';
      this.isDirty = 'No';
      if ((this.finalcount === 0 || this.finalcount < 0) && !this.shouldSave) {
        this.dataEngineering.setApplyFlag(false);
        this.isNextDisabled.emit(false);
      }

      // Emptying already saved changes.
      this.selectedDataTypesToSave = {};
      this.selectedcleanupScalesToSave = {};
      this.DtypeModifiedColumns = {};
      this.ScaleModifiedColumns = {};
      this.DateFormatColumns = {};
      this.processData = JSON.parse(this.dataCleanUpData.processData);
      // old line - this.featureNamesObject = this.processData[this.mappings.featurename];
      // Attribute names should come in sorted
      this.featureNamesObject = this.processData[this.mappings.featurename];
      this.prepopulateFeatures();
      this.sortAttributeList();

      this.unselectedDatatypeFeatures = new Set();
      for (let i = 0; i < this.featureNames.length; i++) {
        const dtfeatures = this.featureNamesObject[this.featureNames[i]][this.mappings.datatype];
        for (const key in dtfeatures) {
          if (key === 'Select Option' && dtfeatures[key].toLowerCase() === 'true') {
            this.unselectedDatatypeFeatures.add(this.featureNames[i]);
          }
        }
      }

      for (let i = 0; i < this.featureNames.length; i++) {
        const dtfeatures = this.featureNamesObject[this.featureNames[i]][this.mappings.datatype];
        const dtqualityscore = this.featureNamesObject[this.featureNames[i]][this.mappings.dataqualityscore];
        if (dtqualityscore * 1 < 50) {
          if (this.uniqueIdentifer !== this.featureNames[i]) { // Exception: Unique Identifier will not be removed even its data quality < 50
            if (dtfeatures !== 'Id') {
              this.featuresNamesLessDataQuality.add(this.featureNames[i]);
              this.ls.setLocalStorageData('lessdataquality', Array.from(this.featuresNamesLessDataQuality));
            }
          }
        }
      }

      this.correlationtoRemove = this.processData.CorrelationToRemove;
      if (Object.keys(this.correlationtoRemove).length !== 0) {

        this.getHighlyCorrelatedVlues(this.correlationtoRemove);
      }

      if (this.processData.hasOwnProperty('UnchangedDtypeColumns')) {
        if (!this.coreUtilsService.isEmptyObject(this.processData.UnchangedDtypeColumns)) {
          const notcreated = JSON.stringify(this.processData.UnchangedDtypeColumns);
          const message = notcreated.substring(2, notcreated.length - 2);
          this.ns.warning(message);
          this.isDirty = 'Yes'; // Fix for Next disabled
        }
      }
      this.unsubscribe();
    } else {
      this.noData = 'Format from server is not Recognised';
      this.ns.error(`Error occurred:
       Due to some backend data process
        the relevant data could not be produced.
         Please try again while we troubleshoot the error`);
      this.unsubscribe();
    }
  }

  isEmptyObject(correlationtoRemove) {
    return (correlationtoRemove && (Object.keys(correlationtoRemove).length !== 0));
  }

  getHighlyCorrelatedVlues(correlationToRemoveFromGtid) {
    this.prescriptionCorrelatedVal = correlationToRemoveFromGtid;
    this.RetainColumns(correlationToRemoveFromGtid);
    const valuecount = Object.values(this.prescriptionCorrelatedVal);
    this.highlyCorrelatedKeys = Object.keys(correlationToRemoveFromGtid);
    for (let i = 0; i < this.highlyCorrelatedKeys.length; i++) {
      for (let k = i; k < valuecount.length; k++) {
        if (JSON.stringify(valuecount[k]) === '[]') {
          this.prescritiveFlag = true;
        } else {
          const datacount = JSON.stringify(valuecount[k]);
          if (datacount.length > 0) {
            this.valueexit = true;
          }
        }
      }
    }
    if (this.valueexit === true) {
      this.prescritiveFlag = false;
    }
  }


  onChecked(elementRef) {
    const value = elementRef.value.trim();
    const index = value.indexOf(':');
    const sbstr = value.substr(0, index);
    this.tempFeaturesNamestobeFixed = [];
    if (index >= 0) {
      this.retainedColumn[value] = elementRef.checked;
      /* if (elementRef.checked === true) {
        this.tempFeaturesNamestobeFixed.push(sbstr);
      }
      if (elementRef.checked === false) {
        const indx = this.tempFeaturesNamestobeFixed.indexOf(sbstr);
        this.tempFeaturesNamestobeFixed.splice(indx, 1);
      } */
    } else {
      this.retainedColumn[value] = elementRef.checked;
      /* if (elementRef.checked === true) {
        this.tempFeaturesNamestobeFixed.push(sbstr);
      }
      if (elementRef.checked === false) {
        const indx = this.tempFeaturesNamestobeFixed.indexOf(sbstr);
        this.tempFeaturesNamestobeFixed.splice(indx, 1);
      } */
    }
    let countOfSelected = 0;
    for (const columnName in this.retainedColumn) {
      if (this.retainedColumn[columnName] === false) {
        this.tempFeaturesNamestobeFixed.push(columnName);
        countOfSelected++;
      } else if (this.retainedColumn[columnName] === false) {
        const indx = this.tempFeaturesNamestobeFixed.indexOf(columnName);
        this.tempFeaturesNamestobeFixed.splice(indx, 1);
      }
    }

    if (Object.keys(this.retainedColumn).length === countOfSelected) {
      this.tempFeaturesNamestobeFixed = [];
    }

    this.featuresNamestobeFixed = new Set(this.tempFeaturesNamestobeFixed);
  }

  RetainColumns(correlatedColumns) {
    this.retainedColumn = {};
    for (const key in correlatedColumns) {
      this.retainedColumn[key.trim()] = false;
      const columnname = correlatedColumns[key];
      for (const key2 in columnname) {
        this.retainedColumn[columnname[key2].trim()] = false;
      }
    }
    // this.tempFeaturesNamestobeFixed = Object.keys(this.retainedColumn);
    console.log(this.retainedColumn);
  }


  onCheckedIdDatatype(value: string) {
    this.featuresNamestobeFixed.add(value);
  }

  getDataTypes(featureName) {
    let dataTypes = Object.keys(this.featureNamesObject[featureName][this.mappings.datatype]);
    const dt = this.featureNamesObject[featureName][this.mappings.datatype];

    if (this.featurenamecount < this.featureNames.length) {
      for (let i = 0; i < this.featureNames.length; i++) {
        for (const keys in dt) {
          if (keys === 'Select Option' && dt[keys].toLowerCase() === 'true') {
            this.dataEngineering.setApplyFlag(true); this.isNextDisabled.emit(true);
            this.countOfUnselectedDataType++;
            this.redDatatype = true;
            this.finalcount = (this.countOfUnselectedDataType / this.featureNames.length);
          } else { this.redDatatype = false; }
        }
        if (this.featureNames[i] === this.targetColumn) {
          for (const keys in dt) {
            if ((keys === 'Text' || keys === 'Id' || keys === 'datetime' || keys === 'datetime64[ns]')
              && dt[keys].toLowerCase() === 'true') {
              this.dataEngineering.setApplyFlag(true);
              this.isNextDisabled.emit(true);
            } else { this.dataEngineering.setApplyFlag(false); this.isNextDisabled.emit(false); }
          }
        }
      }
      this.featurenamecount++;
    }

    for (let i = 0; i < this.featureNames.length; i++) {
      const dtfeatures = this.featureNamesObject[this.featureNames[i]][this.mappings.datatype];
      for (const key in dtfeatures) {
        if (key === 'Select Option' && dtfeatures[key].toLowerCase() === 'true') {
          this.unselectedDatatypeFeatures.add(this.featureNames[i]);
        }
      }
    }


    // Enhancement :: Added validations in data curation

    let featureDataType; // To get the selected datatype as true
    for (const keys in dt) {
      if ((keys === 'Text' || keys === 'Id' || keys === 'datetime' || keys === 'datetime64[ns]')
        && dt[keys].toLowerCase() === 'true') {
        featureDataType = keys;
      }
    }
    const featureUniqueValue = this.featureNamesObject[featureName].Unique * 1; // Changing to number for comparing ===
    if (featureDataType === 'Text' && featureUniqueValue > 60) {
      // dataTypes = ['Text'];
      const indx = dataTypes.indexOf('category');
      dataTypes.splice(indx, 1);
      if (featureUniqueValue !== 100) {
        const indx1 = dataTypes.indexOf('Id');
        dataTypes.splice(indx1, 1);
      }
    }

    if (featureDataType === 'Id' && featureUniqueValue === 100.0) {
      // console.log(featureUniqueValue);
      // dataTypes = ['Id'];
      const indx = dataTypes.indexOf('category');
      dataTypes.splice(indx, 1);
    }

    if (featureDataType !== 'Text' && featureUniqueValue < 100.0) {
      const indx = dataTypes.indexOf('Id');
      dataTypes.splice(indx, 1);
    }

    if (this.featureNamesObject[featureName].Data_Quality_Score * 1 < 50) {
      if (this.uniqueIdentifer !== featureName) { // Exception: Unique Identifier will not be removed even its data quality < 50
        if (featureDataType !== 'Id') {
          this.featuresNamesLessDataQuality.add(featureName);
          this.ls.setLocalStorageData('lessdataquality', Array.from(this.featuresNamesLessDataQuality));
        }
      }
    }

    this.isNextEnabled();
    return dataTypes;
  }

  selectedDateFormat(key, value, dateformat) {
    this.DateFormatColumns[key] = {
      'type': value,
      'format': dateformat
    };
  }

  selectedDatatypeOption(key, value, e) {
    const condition = 'True';
    const unselectedDatatypeFeaturesArray = Array.from(this.unselectedDatatypeFeatures);
    // validation for datatype of targetcolumn
    if (key === this.targetColumn && (value === 'Text' || value === 'Id' || value === 'datetime' || value === 'datetime64[ns]')) {
      this.dataEngineering.setApplyFlag(true);
      this.isNextDisabled.emit(true);
      this.shouldSave = true;
      this.ns.error('The target column should be either Categoric or Numeric');
    } else {
      this.shouldSave = false;
      this.dataEngineering.setApplyFlag(false);
      this.isNextDisabled.emit(false);
    }

    // code to decrease the count of unselected data type features
    for (let i = 0; i < unselectedDatatypeFeaturesArray.length; i++) {
      if (key === unselectedDatatypeFeaturesArray[i] && value.toLowerCase() !== 'Select Option') {
        this.finalcount--;
      }
    }
    // end of code to decrease the count

    if (this.countOfUnselectedDataType === 0 && !this.shouldSave) {
      this.dataEngineering.setApplyFlag(false);
    }
    this.isDirty = 'Yes';
    this.ns.warning('Please Click on Save to update all the changes');
    this.isNextEnabled();
    this.selectedDataTypesToSave[key] = [value, condition];
  }

  getDefaultSeletedDataType(featureName, option) {
    let tempOption: string = this.featureNamesObject[featureName][this.mappings.datatype][option];
    tempOption = tempOption ? tempOption.toLowerCase() : '';
    if (option === 'Id' && tempOption === 'true') {
      this.idDataTypeForPrescription.add(featureName);
      this.attributesWithId = Array.from(this.idDataTypeForPrescription).join(',');
    }
    if (option === 'datetime64[ns]' && this.modelType === 'TimeSeries' && tempOption === 'true') {
      this.disabledDateTimeForTimeSeriesModel[featureName] = true;
    }
    return tempOption === 'true' ? true : false;
  }

  getSelectedDateFormat(featureName, format) {
    this.selectdefaultDateFormat = this.payloaddata['Datetimeformat'];
    if (this.selectdefaultDateFormat !== undefined && this.selectdefaultDateFormat !== null) {
      const temp = this.selectdefaultDateFormat[featureName]['format'];
      return (format === temp) ? true : false;
    }
  }

  getUnselectedDatatype(featureName) {
    const dataTypes = Object.keys(this.featureNamesObject[featureName][this.mappings.datatype]);
    const dt = this.featureNamesObject[featureName][this.mappings.datatype];
    for (const keys in dt) {
      if (keys === 'Select Option' && dt[keys].toLowerCase() === 'true') {
        this.redDatatype = true;
        return this.redDatatype;
      } else {
        this.redDatatype = false;
        return this.redDatatype;
      }
    }
  }

  ngAfterContentInit() {
    if (this.countOfUnselectedDataType === 0 && !this.shouldSave) {
      this.dataEngineering.setApplyFlag(false);
      this.isNextDisabled.emit(false);
    }
    if (this.shouldSave) {
      this.dataEngineering.setApplyFlag(true);
      this.isNextDisabled.emit(true);
    }
  }

  getUniqueValue(featureName) {
    return isNaN(this.featureNamesObject[featureName].Unique) ? this.featureNamesObject[featureName].Unique : parseFloat(this.featureNamesObject[featureName].Unique).toFixed(this.decimalPoint);
  }

  getCorrelation(featureName) {
    const highlyCorrelatedVlues = Object.keys(this.featureNamesObject[featureName][this.mappings.correlation])
      .map(item => this.featureNamesObject[featureName][this.mappings.correlation][item]);
    const correlations = highlyCorrelatedVlues.join(', ');
    return correlations.toString();
  }

  getScales(featureName) {
    const scales = Object.keys(this.featureNamesObject[featureName][this.mappings.scale]);
    return scales.length === 0 ? this.defaultScales : scales;
  }

  getDefaultSeletedScale(featureName, option) {
    const attribute = this.featureNamesObject[featureName];
    const tempOptions = attribute[this.mappings.scale];
    const tempOption = tempOptions[option] ? tempOptions[option].toLowerCase() : '';

    return tempOption === 'true' ? true : false;
  }

  getMissingValue(featureName) {
    const MissingValue = this.featureNamesObject[featureName][this.mappings.missingvalues];
    if (MissingValue > 50) {
      this.fixMissingValue.add(featureName + ':' + MissingValue);
    }
    return isNaN(MissingValue) ? MissingValue : parseFloat(MissingValue).toFixed(this.decimalPoint);
  }

  getOutlier(featureName) {
    return isNaN(this.featureNamesObject[featureName][this.mappings.outlier]) ? this.featureNamesObject[featureName][this.mappings.outlier] : parseFloat(this.featureNamesObject[featureName][this.mappings.outlier]).toFixed(this.decimalPoint);
  }

  getBalanced(featureName) {
    return this.featureNamesObject[featureName].Balanced;
  }

  getSKewNess(featureName) {
    return this.featureNamesObject[featureName][this.mappings.skewness];
  }

  getDataQualityScore(featureName) {
    let dataQualityScore = this.featureNamesObject[featureName][this.mappings.dataqualityscore];
    dataQualityScore = parseFloat(dataQualityScore).toFixed(2);

    // Columns with <30% data quality
    if (dataQualityScore < 30) {
      if (Array.from(this.idDataTypeForPrescription).find(features => features)) { } else {
        this.fixDataQuality.add(featureName + ':' + dataQualityScore);
      }
    }
    const dt = this.featureNamesObject[featureName][this.mappings.datatype];
    let featureDataType; // To get the selected datatype as true
    for (const keys in dt) {
      if ((keys === 'Text' || keys === 'Id' || keys === 'datetime' || keys === 'datetime64[ns]')
        && dt[keys].toLowerCase() === 'true') {
        featureDataType = keys;
      }
    }
    if (featureDataType == 'Id') {
      return 'NA';
    }
    return isNaN(dataQualityScore) ? dataQualityScore : parseFloat(dataQualityScore).toFixed(this.decimalPoint);
  }

  getClassForDataQualityScore(featureName) {
    const dataQualityScore = this.featureNamesObject[featureName][this.mappings.dataqualityscore];
    const dataTypes = Object.keys(this.featureNamesObject[featureName][this.mappings.datatype]);
    if (dataTypes.length === 0) {
      return 'ingrAI-data-quality-failed-by-datatype';
    }
    if (dataQualityScore <= 0) {
      return 'ingrAI-data-quality-NA';
    }
    if (dataQualityScore > 0 && dataQualityScore < 50) {
      return 'ingrAI-data-quality-failed';
    } else if (dataQualityScore >= 50 && dataQualityScore < 80) {
      return 'ingrAI-data-quality-average';
    } else if (dataQualityScore >= 80) {
      return 'ingrAI-data-quality-success';
    }
  }

  retry() {
    this.timerSubscripton = timer(10000).subscribe(() => this.getDataCleanUpData());
    return this.timerSubscripton;
  }

  retrySave() {
    this.timerSubscriptonForSave = timer(10000).subscribe(() => this.saveDataAfterCleanUp());
    return this.timerSubscriptonForSave;
  }

  selectedScaleOption(key, value) {
    this.selectedcleanupScalesToSave[key] = value;
    this.isDirty = 'Yes';
    this.ns.warning('Please Click on Save to update all the changes')
  }

  getStylesforTooltip() {
    const top = this.topValueForTooltip + 210 * (this.topValueForTooltip / 4) + '%';
    return top;
  }

  openHelper(featureName) {
    // console.log(event.y);
    // this.topValueForTooltip = i;
    this.dataQualityInfo = this.featureNamesObject[featureName][this.mappings.q_info];
    // this.showpopover.nativeElement.classList.add('show');
  }

  closePopover() {
    this.showpopover.nativeElement.classList.remove('show');
  }

  checkRetrainStatus() {
    if (!this.readOnly) {
      if (this.IsModelTrained !== undefined) {
        if (this.IsModelTrained === true) {
          this.modalRef = this._modalService.show(this.confirmChangesOnDataCuration, this.config);
        } else {
          this.saveDataAfterCleanUp();
        }
      } else {
        this.saveDataAfterCleanUp();
      }
    }
  }

  confirm() {
    this.modalRef.hide();
    this.saveDataAfterCleanUp();
  }
  // confirmation popup cancle event
  decline() {
    this.modalRef.hide();
  }

  saveDataAfterCleanUp() {
    this.pageInfo = 'UpdateDataCleanUp';
    if (this.coreUtilsService.isEmptyObject(this.selectedDataTypesToSave)
      && this.coreUtilsService.isEmptyObject(this.selectedcleanupScalesToSave)
      && this.coreUtilsService.isEmptyObject(this.DtypeModifiedColumns) && this.coreUtilsService.isEmptyObject(this.ScaleModifiedColumns)
      && this.coreUtilsService.isEmptyObject(this.DateFormatColumns)) {
      this.ns.error('No changes made to be saved');
    } else {
      this.saveSubscription = this.dataEngineering.postDataCleanup(this.selectedDataTypesToSave, this.selectedcleanupScalesToSave,
        this.correlationId, this.DtypeModifiedColumns, this.ScaleModifiedColumns, this.userId, this.pageInfo, this.DateFormatColumns).pipe(

          switchMap(
            data => {
              let tempData = {};
              if (this.IsJsonString(data)) {
                const parsedData = JSON.parse(data);
                tempData = parsedData;
              } else if (data.constructor === Object) {
                tempData = data;
              } else if (data.constructor === String) {
                this.noData = data.toString();
                this.ns.success(data);
              }

              if (tempData.hasOwnProperty('message')) {
                // this.dataTypeChange = 'UpdateDataCleanUpSuccess';
                // this.isDirty = 'No';
                return this.dataEngineering.postDataCleanup(this.selectedDataTypesToSave,
                  this.selectedcleanupScalesToSave, this.correlationId, this.DtypeModifiedColumns,
                  this.ScaleModifiedColumns, this.userId, this.pageInfo, this.DateFormatColumns);
              } else if (data.Progress && data.Status) {
                return of(tempData);
              } else {
                this.noData = 'Format from server is not Recognised';
                let msg = 'Error occurred: Due to some backend data process the relevant data could not be produced.';
                msg = msg + ' Please try again while we troubleshoot the error';
                this.ns.error(msg);
                return EMPTY;
              }
            }),
          catchError(error => {
            return EMPTY;
          })
        ).subscribe(data => {
          /* let currentIndex = 1;
          let breadcrumbIndex = 1;
          this.coreUtilsService.disableTabs(currentIndex,breadcrumbIndex); */
          this.setAttributesafterSave(data)
        });
    }
  }

  saveAs($event) {
    const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this.ps.clone(this.correlationId, modelName);
        } else { return EMPTY; }
      }),

      tap(data => this.newCorrelationIdAfterCloned = data),

      tap(data => {
        if (data) {
          this.router.navigate(['dashboard/dataengineering/datacleanup'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });
          this.ns.success('Cloned Successfully');
        }
      })
    );
    openTemplateAfterClosed.subscribe();
  }

  fixit() {
    this.status = false;
    if (this.featuresNamestobeFixed.size > 0) {
      this.dataEngineering.fixColumns(this.correlationId, this.featuresNamestobeFixed).subscribe(
        data => {

          // if (data === 'Success. ')  {
          if (data.substring(0, 7) === 'Success') {
            this.ns.success('Data Fixed Successfully.');
            window.location.reload();
          }

        });
    } else {
      this.ns.error('No Column(s) selected to fix');
    }
  }

  ignore() {
    this.status = false;
  }

  getStyles(index) {
    const styles = {
      'top': index + '%',
      'z-index': '101'
    };
    return styles;
  }

  isNextEnabled() {
    // this.isNextDisabled.emit(true);
    const unselectedDatatypeFeaturesArray = Array.from(this.unselectedDatatypeFeatures);
    if (unselectedDatatypeFeaturesArray.length) {
      if (this.finalcount >= 0) {
        if ((this.finalcount === 0 || this.finalcount < 0) && this.shouldSave === false) {
          this.dataEngineering.setApplyFlag(false);
          this.isNextDisabled.emit(false);
          // } else if (this.shouldSave) {
          //   this.dataEngineering.setApplyFlag(true);
        } else if ((this.finalcount === 0 || this.finalcount < 0) && this.shouldSave === true) {
          this.dataEngineering.setApplyFlag(true);
          this.isNextDisabled.emit(true);
        }
      } else {
        // const unselectedDatatypeFeaturesArray = Array.from(this.unselectedDatatypeFeatures);
        if (this.dataTypeChange === 'UpdateDataCleanUpSuccess') {
          this.dataEngineering.setApplyFlag(false);
          this.isNextDisabled.emit(false);
        } else {
          this.dataEngineering.setApplyFlag(true);
          this.isNextDisabled.emit(true);
        }
      }
    } else {
      this.dataEngineering.setApplyFlag(false);
      this.isNextDisabled.emit(false);
      // Added for Error Handling 
      if (this.processData === null) {
        this.dataEngineering.setApplyFlag(true);
        this.isNextDisabled.emit(true);
      }
    }

    if (this.isDirty === 'Yes') {
      this.dataEngineering.setApplyFlag(true);
      this.isNextDisabled.emit(true);
    }
    if (this.featureNames) {
      const index = this.featureNames.findIndex(name => name === this.targetColumn);
      if (this.IsCustomColumnSelected === 'True') {
        const value: boolean = (index < 0)
        this.dataEngineering.setApplyFlag(value);
        this.isNextDisabled.emit(value);

      }
    }
  }

  private disableTabs() {
    // this.ps.getColumnsdata.IsModelTrained
    const classnameDisabled = 'anchor-disabled';
    const classnameEnabled = 'anchor-enabled';
    const nativeElemnt = !this.coreUtilsService.isNil(this.eleRef.nativeElement) ? this.eleRef.nativeElement : null;
    const parentEle = !this.coreUtilsService.isNil(nativeElemnt) ? nativeElemnt.parentElement : null;
    const nestedParentEle = !this.coreUtilsService.isNil(parentEle) ? parentEle.parentElement : null;
    if (!this.coreUtilsService.isNil(nestedParentEle)) {
      /* const allLinks = nestedParentEle.children[0].querySelectorAll('a');
      const submenulinks = nestedParentEle.children[2].querySelectorAll('a');
       for (let index = 0; index < submenulinks.length; index++) {
        if (submenulinks[index].text === 'DATA TRANSFORMATION') {
          if (this.dataCleanUpData) {
            submenulinks[index].className = 'ingrAI-sub-nav-button ingrAI-sub-nav-active ' + classnameEnabled;
          } else {
            submenulinks[index].className = 'ingrAI-sub-nav-button ' + classnameDisabled;
          }
        }
      } */
    }
  }

  private sortAttributeList() {
    const sortedData = Object.entries(this.featureNamesObject).sort(function (a, b) { return a > b ? 1 : -1; });
    const newObj = {};
    for (let index = 0; index < sortedData.length; index++) {
      const key = sortedData[index][0];
      const value = sortedData[index][1];
      newObj[key] = value;
    }
    this.featureNamesObject = newObj;
    // Attribute names should come in sorted
    this.fixedFeatureNamesObject = JSON.parse(JSON.stringify(this.featureNamesObject)); // deep cloning
    this.featureNames = Object.keys(this.featureNamesObject);
    const index = this.featureNames.findIndex(name => name === this.targetColumn);
    if (this.IsCustomColumnSelected === 'True' && index < 0) {
      // this.dataEngineering.setApplyFlag(true);
      this.ns.warning(' Please create ' + this.targetColumn + ' attribute from "Add Feature"')
    }

    for (let i = 0; i < this.featureNames.length; i++) {
      const dtfeatures = this.featureNamesObject[this.featureNames[i]][this.mappings.datatype];
      const dtqualityscore = this.featureNamesObject[this.featureNames[i]][this.mappings.dataqualityscore];
      if (dtqualityscore * 1 < 50) {
        if (this.uniqueIdentifer !== this.featureNames[i]) { // Exception: Unique Identifier will not be removed even its data quality < 50
          if (dtfeatures !== 'Id') {
            this.featuresNamesLessDataQuality.add(this.featureNames[i]);
            this.ls.setLocalStorageData('lessdataquality', Array.from(this.featuresNamesLessDataQuality));
          }
        }
      }
    }

    const isContainsDateTime = this.featureNames.some(val => this.featureNamesObject[val].Datatype['datetime64[ns]'] === 'True');

    if (isContainsDateTime && this.modelType !== 'TimeSeries') {
      this.ns.warning('Date columns will not be used as input for model training');
    }

  }

  public openAddFeatureTypeSelection(addFeatureManualModal: TemplateRef<any>) {
    if (this.modelType !== 'TimeSeries') {
      this.uts.usageTracking('Data Engineering', 'Add Feature');
      this.modalRefForManual = this._modalService.show(addFeatureManualModal, this.addfeatureconfig);
    }
  }

  public reloadDataCleanUp(data) {
    this.checkFeatureCreatedOrNot(data[0]);
    this.modalRefForManual.hide();
    if (data[1] === 'created' && data[0].processData) {
      this.featuresAddedbyUser = (data[0].processData) ? JSON.parse(data[0].processData) : {};
      this.setAttributes(data[0]);
    }
  }

  public setVariableRequiredForAddFeatureComponent(data) {
    this.modelType = data.ModelType;
    if (data.FeatureSelectionData && data.FeatureSelectionData.hasOwnProperty('Existing_Features')) {
      this.Existing_Features_Disabled = new Set(data.FeatureSelectionData.Existing_Features);
    }
    this.IsCustomColumnSelected = (data.IsCustomColumnSelected) ? data.IsCustomColumnSelected : 'False'
    if (data.CleanedUpColumnList && data.FeatureDataTypes) {
      this.featureDataTypes = data.FeatureDataTypes;
      this.columnList = data.CleanedUpColumnList.filter(val => ![data.TargetColumn, data.TargetUniqueIdentifier].includes(val));
      this.columnListNoText = this.columnList.filter(val => !data.TextTypeColumnList.includes(val));
    }
  }

  public prepopulateFeatures() {
    if (this.processData.hasOwnProperty('NewAddFeatures')) {
      this.featuresAddedbyUser = this.processData['NewAddFeatures'];
    }
  }

  public checkFeatureCreatedOrNot(data) {
    if (data.FeatureSelectionData && data.FeatureSelectionData.hasOwnProperty('Features_Created')
      && data.FeatureSelectionData.hasOwnProperty('Feature_Not_Created')) {
      this.Existing_Features_Disabled = new Set(data.FeatureSelectionData.Existing_Features);
      let featurenotcreated = data.FeatureSelectionData.Feature_Not_Created;
      const index = Object.keys(featurenotcreated).findIndex(name => name === this.targetColumn);
      if (Object.keys(featurenotcreated).length !== 0 && this.IsCustomColumnSelected === 'True' && index > -1) {
        this.ns.error('Custom target attribute is not created' + featurenotcreated[this.targetColumn])
      }
      else {
        if (data.FeatureSelectionData.Features_Created.length > 0) {
          const message = data.FeatureSelectionData.Features_Created.join(' ')
          this.ns.success('Feature created successfully ' + message);
          if (this.IsCustomColumnSelected === 'True' && message.includes(this.targetColumn)) {
            this.dataEngineering.setApplyFlag(false);
          } else if (this.IsCustomColumnSelected === 'True' && !message.includes(this.targetColumn)) {
            // this.ns.error('Custom target feature not created')
            this.dataEngineering.setApplyFlag(true);
          }
        }
        let notcreated = data.FeatureSelectionData.Feature_Not_Created;
        if (Object.keys(notcreated).length !== 0) {
          notcreated = JSON.stringify(notcreated);
          const message = notcreated.substring(2, notcreated.length - 2);
          // this.ns.error('Feature not created ' + message);
          this.ns.error(message);
        }
      }
    }
  }

  public disableNewAddedAttribute(featureName) {

  }

  accessDenied() {
    this.ns.error('Access Denied');
  }

  next(lessdataquality?) {
    const data = this.ls.getFeatureNameDataQualityLow();
    if (data === undefined || data === '') {
      this.coreUtilsService.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
      this.customRouter.redirectToNext();
    } else {
      this.confirmPopup();
    }
  }

  confirmPopup() {
    if (this.pstDataSource === 'InstaML') {
      this.ls.setLocalStorageData('lessdataquality', '');
      this.aus.loadingEnded();
      this.coreUtilsService.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
      this.customRouter.redirectToNext();
    } else {
      if (!this.featuresNamesLessDataQuality.has(this.targetColumn)) {
        this.aus.loadingStarted();
        this.dataEngineering.fixColumns(this.ls.getCorrelationId(), this.ls.getFeatureNameDataQualityLow()).subscribe(data => {
          if (data.substring(0, 7) === 'Success') {
            this.ls.setLocalStorageData('lessdataquality', '');
            this.aus.loadingEnded();
            this.coreUtilsService.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
            this.customRouter.redirectToNext();
          }
          this.aus.loadingEnded();
        });
      } else if (this.featuresNamesLessDataQuality.has(this.targetColumn)) {
        //  this.des.setApplyFlag(true);
        // this.isNextDisabled = false;
        this.message = 'Attribute ' + this.targetColumn + ' contains less than 50% data quality score ,' + 'Kindly go back and update the data again';
        this.ns.warning(this.message);
        this.isNextDisabled.emit(false);
        this.aus.loadingEnded();
      }
    }
  }

  previous() {
    if (localStorage.getItem('fromSource') === 'FDS' || localStorage.getItem('fromSource') === 'PAM') {
      if (this.router.url.includes('/dashboard/dataengineering/datacleanup?')) {
        const modelname = this.ls.getModelName();
        const modelCategory = this.ls.getModelCategory();
        let requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelName=' + modelname + '&pageSource=true';
        if (modelCategory !== null && modelCategory !== undefined && modelCategory !== '') {
          requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelCategory=' + modelCategory +
            '&displayUploadandDataSourceBlock=true&modelName=' + modelname;
        }
        this.customRouter.previousUrl = requiredUrl;
      }
    } else {
      if (this.customRouter.urlAfterRedirects === '/dashboard/dataengineering/datacleanup'
        || this.router.url === '/dashboard/dataengineering/datacleanup') {
        const modelname = this.ls.getModelName();
        const modelCategory = this.ls.getModelCategory();
        let requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelName=' + modelname + '&pageSource=true';
        if (modelCategory !== null && modelCategory !== undefined && modelCategory !== '') {
          requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelCategory=' + modelCategory +
            '&displayUploadandDataSourceBlock=false&modelName=' + modelname;
        }
        this.customRouter.previousUrl = requiredUrl;
      }
    }
    let currentIndex = 0;
    let breadcrumbIndex = 0;
    this.coreUtilsService.disableTabs(this.currentIndex, this.breadcrumbIndex, true, null);
    this.isNextDisabled.emit(true);
    this.customRouter.redirectToPrevious();
  }
}
