import { Component, OnInit, AfterViewInit, ElementRef, Inject, TemplateRef, ViewChild, Output, EventEmitter } from '@angular/core';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { tap, switchMap, catchError, find } from 'rxjs/operators';
import { throwError, of, timer, EMPTY } from 'rxjs';
import { DialogService } from 'src/app/dialog/dialog.service';
import { TemplateNameModalComponent } from '../../../template-name-modal/template-name-modal.component';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { Router } from '@angular/router';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { DataEncodingComponent } from './data-encoding/data-encoding.component';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';

@Component({
  selector: 'app-preprocess-data',
  templateUrl: './preprocess-data.component.html',
  styleUrls: ['./preprocess-data.component.scss']
})
export class PreprocessDataComponent implements OnInit, AfterViewInit {

  @ViewChild(DataEncodingComponent, { static: false }) public dataencoding: DataEncodingComponent;
  @ViewChild('confirmChangesOnDataTransformation', { static: true }) confirmChangesOnDataTransformation;
  modalRefForType: BsModalRef | null;
  modalRefForManual: BsModalRef | null;
  config = {
    ignoreBackdropClick: true,
    class: 'preprocess-addfeature-manualmodal'
  };

  allColumnMappingArray: any = [];
  missingValues: {};
  missingValueAttributes = [];
  missingValueOptions = [];
  defaultMissingValueOptions = {};
  missingValuesCustomValue: {} = {};
  featureDataTypes = {}; // Missing DataTypes

  // clickedIndex = -1;
  filters: {};
  filterAttributes = [];
  defaultFilterOptions = {};
  prepopulateAutoBinningColumns: {} = {};
  categoricalcolumns: {} = {};

  BinningColumns: {} = {};
  //  dataModification
  // dataModification: {} = {};
  dataModification: {};
  dataModificationAttributes = [];
  dataModificationOutlierCheckBoxInputs = ['Mean', 'Mode', 'Median', 'CustomValues'];
  dataModificationOutlierOptions: {} = {};
  dataModificationSkewnessOptions: {} = {};
  bininngData = {};

  newBinName = '';
  defaultModificationOptions = {};
  defaultModificationOptionsSkewness = {};

  //  dataEncoding
  dataEncoding = {};
  dataEncodingAttributes = [];
  dataEncodingOptions = [];
  bininngDataAttributes: string[];
  bininngDataOptions: {} = {};
  binning = {
    names: new Set(),
    allBinnedColumns: new Set()
  };

  modalRef: BsModalRef;
  config2 = {
    backdrop: true,
    class: 'deploymodle-confirmpopup'
  };

  //  others
  status = true;
  preProcessData = {};
  dataToServer = {
    'MissingValues': {},
    'Filters': {},
    'DataEncoding': {},
    'DataModification': {
      'Skewness': {},
      'Outlier': {},
      'binning': {},
      'RemoveColumn': {}
    },
    'Smote': {},
    'SmoteMulticlass': {}
  };

  isSmoteVisible = true;
  smoteFlag: boolean = false;

  @ViewChild('smoteFlag', { static: false }) smoteElement: ElementRef;

  autoBinning: any;
  autoBinningDisableColumns: any;
  ismulticlasssmote: boolean;
  binningDatatoServer = {};

  correlationId = '';
  attributes: string[];

  numericalData: {};
  categoricalData = {};

  mappings = {
    missingValues: 'MissingValues',
    filters: 'Filters',
    dataEncoding: 'DataEncoding',
    correlationId: 'CorrelationId',
    Outlier: 'Outlier',
    targetcolumn: 'TargetColumn'
  };

  selectedsubCats = {};

  isLoading = true;
  errorOccured = false;
  newModelName: any;
  newCorrelationIdAfterCloned: {};
  dataModificationSkewnessCustomValue: {} = {};
  dataModificationOutlierCustomValue: {} = {};
  displayHelp: any;
  priscriptionData: {} = {};
  priscriptionText: {}[];
  priscriptioncheckBoxValues: string[];

  dataTobeFixed = [];
  selectedBinningValues: {} = {};

  //  copyOfFilterAttributes: any[]; unused

  BusinessProblem: '';
  DataSource: '';
  ModelName: '';
  userID: string;
  timerSubscripton: any;
  applySubscription: any;
  noData: string;
  targetColumn: string;
  disableDropdown;
  selectedOptionsCount = 0;
  nextDisabled = false;
  pythonProgress;
  pythonProgressStatus;
  showDataIcon = false;
  applyFlag = false;
  problemTypeFlag = false;
  modelType: any;
  autobinningcolumns: any;

  radioTextBox: {} = {};
  displayCustomValueInput = false;
  index: any;
  displayMessage = true;
  subscription: any;
  paramData: any;
  flagInstaML = true;
  addFeatureTypeSelection = ['Manual', 'Automated'];
  deliveryTypeName;
  columnList = [];
  Filterspayload: any;
  columnListNoText = [];
  allColumns = [];
  targetColumnAndUniqueIdentifer = [];
  // For NLP
  TextTypeColumnList = [];
  TextTypeColumnListAttributesData = {};
  RemoveFilterColumn: any;
  featureAdded = {};
  disableFix = false;
  IsModelTrained = false;
  silhouette_graph;
  autobinningcolsname: {} = {};
  multiclassFlag: boolean;
  isBinningFlag: boolean;
  readOnly;

  accordian = {
    'acc01': true,
    'acc02': true,
    'acc03': true,
    'acc04': true,
    'acc05': true,
    'expandAll': true
  }
  @Output() isNextDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() isTimeSeries: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() isApplyDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();

  currentIndex = 1;
  breadcrumbIndex = 1;

  datedInputOnly = false;
  inputColumns = [];
  influencersColumn = [];

  constructor(private des: DataEngineeringService, private ls: LocalStorageService,
    private dialogService: DialogService, private utils: CoreUtilsService,
    private ps: ProblemStatementService,
    private router: Router, private ns: NotificationService, private aus: AppUtilsService,
    private cus: CoreUtilsService, private _modalService: BsModalService,
    @Inject(ElementRef) private eleRef: ElementRef, private uts: UsageTrackingService,
    private customRouter: CustomRoutingService) { }

  ngOnInit() {
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.isBinningFlag = false;
    this.isLoading = true;
    this.correlationId = this.ls.getCorrelationId();
    this.des.setApplyFlag(true);
    this.isNextDisabled.emit(true);
    this.uts.usageTracking('Data Engineering', 'Data transformation');
    this.isLoading = true;
    this.datedInputOnly = false;
    this.isLoading = true;
    this.ps.getColumnsForMyModels(this.correlationId).subscribe(dataCol => {
      if (dataCol) {
        // this.columnList = dataCol.InputColumns;
        const allColumn = dataCol.ColumnsList;
        allColumn.push(dataCol.TargetColumn);
        this.allColumns = allColumn;
        this.IsModelTrained = dataCol.IsModelTrained;
        this.targetColumnAndUniqueIdentifer = [dataCol.TargetColumn, dataCol.TargetUniqueIdentifier];
        this.isLoading = false;

        dataCol.InputColumns.forEach((columnname) => {
          this.inputColumns.push(columnname);
        });

        this.des.getPreprocessData(this.correlationId)
          .pipe(
            tap(data => this.setData(data)),
            catchError(data => {
              this.isLoading = false; this.errorOccured = true;
              return throwError(data);
            })

          ).subscribe(
            data => this.isLoading = false
          );
      }
    }, error => {
      // this.ns.error('Data is not availalbe in column dropdown');
    });

    this.subscription = this.aus.getParamData().subscribe(paramData => {

      this.paramData = paramData;
    });
  }

  ngAfterViewInit() {
    if (this.priscriptioncheckBoxValues === undefined
      && this.priscriptioncheckBoxValues === undefined
      && this.priscriptionText === undefined) {
      this.status = true;
    }
  }

  setData(data) {
    this.modelType = data.ModelType || '';
    this.preProcessData = data.PreprocessedData || {};
    this.featureDataTypes = data.FeatureDataTypes; // Missing DataTypes
    this.multiclassFlag = data.IsMultiClass;

    if (this.modelType !== 'TimeSeries') {

      this.inputColumns.forEach(element => {
        if (this.featureDataTypes.hasOwnProperty(element)) {
          this.influencersColumn.push(this.featureDataTypes[element]);
        }
      });


      if (this.influencersColumn.length === 1) {
        this.influencersColumn.forEach(element => {
          if (element === 'datetime64[ns]') {
            this.datedInputOnly = true;
          }
        });
      }

      if (this.datedInputOnly) {
        this.ns.warning(`Detected Datetime Influencers only :
          Derive more influencers using ADD FEATURE or add more potential influencers in Problem Statement Page`);
        return 0;
      }
    }

    // this.problemTypeFlag = this.modelType === 'TimeSeries' ? true : false;
    if (this.modelType === 'TimeSeries') {
      this.problemTypeFlag = true;
      this.isTimeSeries.emit(true);
    } else {
      this.problemTypeFlag = false;
      this.isTimeSeries.emit(false);
    }
    if (this.modelType === 'Classification' || this.modelType === 'Multi_Class'
      || this.modelType === 'Regression') {

      let textColumn = new Set(data.TextTypeColumnList);
      this.TextTypeColumnList = Array.from(textColumn);
      this.columnListNoText = this.columnList.filter(val => !this.TextTypeColumnList.includes(val));

      const dataModification = this.preProcessData['DataModification'];
      if (dataModification.hasOwnProperty('TextDataPreprocessing')) {
        this.TextTypeColumnListAttributesData = dataModification['TextDataPreprocessing'];
      }

      if (dataModification.hasOwnProperty('AutoBinning')) {
        this.autoBinning = dataModification['AutoBinning'] || {};
      }

    }

    this.silhouette_graph = data.silhouette_graph; // To be passed as input to Text preprocessing component

    this.BusinessProblem = data.BusinessProblem || '';
    this.DataSource = data.DataSource || '';
    this.ModelName = data.ModelName || '';
    this.correlationId = data[this.mappings.correlationId] || this.correlationId;
    this.dataToServer['correlationId'] = this.correlationId;
    this.deliveryTypeName = data.Category;
    console.log('preprocess Data DataTransformationApplied --', this.preProcessData['DataTransformationApplied']);
    // apply flag
    this.applyFlag = this.preProcessData['DataTransformationApplied'] || this.applyFlag;
    if (this.preProcessData['DataTransformationApplied']) {
      localStorage.setItem('oldCorrelationID', 'null');
    }

    // emitting apply flag to all subscriptions
    this.isNextDisabled.emit(!this.applyFlag);
    this.des.setApplyFlag(!this.applyFlag);
    this.disableTabs();
    this.showDataIcon = this.applyFlag;
    this.ls.setLocalStorageData('applyFlag', this.applyFlag);
    this.isApplyDisabled.emit(this.applyFlag);
    if (this.applyFlag) {
      sessionStorage.setItem('applyFlag', this.applyFlag.toString());
    }
    // target column
    this.targetColumn = this.preProcessData[this.mappings.targetcolumn];
    // data Encoding
    this.dataEncoding = this.preProcessData[this.mappings.dataEncoding] || {};
    this.dataEncodingAttributes = Object.keys(this.dataEncoding);
    // console.log(this.dataEncodingAttributes);
    if (this.dataEncodingAttributes.length === 0) {
      this.displayMessage = false;
    }
    for (const t in this.dataEncoding) {
      if (this.dataEncoding.hasOwnProperty(t)) {
        if (t === this.targetColumn) {
          this.dataEncoding[t]['encoding'] = 'Label Encoding';
        } else if (this.dataEncoding[t]['attribute'] === 'Ordinal' && this.dataEncoding[t]['encoding'] === '') {
          this.dataEncoding[t]['encoding'] = 'Label Encoding';
        } else if (this.dataEncoding[t]['attribute'] === 'Nominal' && this.dataEncoding[t]['encoding'] === '') {
          this.dataEncoding[t]['encoding'] = 'Label Encoding';
        }
      }

    }

    this.dataToServer.DataEncoding = this.dataEncoding;

    // missing Values
    this.missingValues = this.preProcessData[this.mappings.missingValues] || {};

    // filters
    this.filters = this.preProcessData[this.mappings.filters] || {};

    // DataModification

    this.dataModification = this.preProcessData['DataModification'] || {};

    this.priscriptionData = this.dataModification['Prescriptions'] || {};
    // this.priscriptionText = Object.values(this.priscriptionData) || []
    this.priscriptionText = Object.keys(this.priscriptionData).map(item => this.priscriptionData[item]) || [];
    this.priscriptioncheckBoxValues = Object.keys(this.priscriptionData) || [];

    this.prepopulateAutoBinningColumns = data.AutoBinningDisableColumns || {};


    this.categoricalcolumns = data.AutoBinningNumericalColumns || {};


    // SMOTE Related Changes
    if (this.preProcessData['Smote'] !== undefined) {
      this.disableFix = this.preProcessData['Smote']['Flag'] !== 'False';

      this.isSmoteVisible = this.preProcessData['Smote']['Flag'] === 'True' ? true : false;
      this.smoteFlag = this.preProcessData['Smote']['Flag'] === 'True' ? true : false;
      if (this.isSmoteVisible) {
        setTimeout(() => {
          this.smoteElement.nativeElement.checked = this.preProcessData['Smote']['Flag'] === 'True' ? true : false;
        }, 1000);
      }
    }

    if (this.multiclassFlag === true && this.modelType === 'Classification') {
      if (this.preProcessData['SmoteMulticlass'] !== undefined) {
        this.ismulticlasssmote = (this.preProcessData['SmoteMulticlass']['UserConsent'] === 'True'
          && this.preProcessData['SmoteMulticlass']['ChangeRequest'] === 'True') ? true : false;
      }
    }
  }

  getKeyByValue(object, value) {
    if (!object) {
      return '';
    }

    delete object.ChangeRequest;
    delete object.PChangeRequest;
    const result = Object.keys(object).filter(key => object[key] === value);
    return result[0];
  }

  prescriptionOpen() {
    this.status = !this.status;
  }


  toggleHelp() {
    this.displayHelp = !this.displayHelp;
  }

  onChange(tabName, attribute, selectedOption, inputValue) {
    // (tabName, attribute, selectedOption, inputValue)

    if (selectedOption === 'CustomValue') {
      if (inputValue !== null && inputValue !== undefined) {
        this.setDatatoserver(tabName, attribute, selectedOption, inputValue);
      }
    } else {
      this.setDatatoserver(tabName, attribute, selectedOption, null);
    }
  }

  // setIndex(index) {
  //   this.clickedIndex = index;
  // }

  onFixIt() {
    this.des.fixColumnsDataTransformation(this.correlationId).subscribe(
      data => {
        this.ns.success('Data fixed successfully');
      }
    );
  }

  ignore() {
    this.status = false;
  }

  setDatatoserver(tabName, attribute, selectedValue, customValue) {

    const dataModification = this.preProcessData['DataModification'];
    this.dataToServer['correlationId'] = this.correlationId;

    if (tabName === 'Skewness') {
      if (selectedValue === 'CustomValue') {
        this.dataModificationSkewnessCustomValue[attribute] = customValue;
      }
      this.dataToServer.DataModification[tabName][attribute] = {};
      this.dataToServer.DataModification[tabName][attribute][selectedValue] = customValue || 'True';
    } else if (tabName === 'Outlier') {
      if (selectedValue === 'CustomValue') {
        this.dataModificationOutlierCustomValue[attribute] = customValue;
      }

      this.dataToServer.DataModification[tabName][attribute] = {};
      this.dataToServer.DataModification[tabName][attribute][selectedValue] = customValue || 'True';
    } else if (tabName === 'RemoveColumn') {
      this.dataToServer.DataModification[tabName][attribute] = {};
      this.dataToServer.DataModification[tabName][attribute][selectedValue] = customValue || 'True';
    } else if (tabName === 'Interpolation') {
      this.dataToServer.DataModification[attribute] = selectedValue;
    } else if (tabName === 'Filters') {
      if (attribute === this.targetColumn && selectedValue.length < 2 && selectedValue.length !== 0) {
        this.isSmoteVisible = false;
        this.dataToServer['Smote']['Flag'] = 'False';
        // this.ns.error('Please select atleast 2 filters to be applied to Target Column ');
        this.ns.error('Please select atleast 2 filters for target column');
        // BugFix for 995519
        this.dataToServer[tabName][attribute].forEach((element, index) => {
          if (Object.keys(element)[0] !== selectedValue[0]) {
            this.dataToServer[tabName][attribute].splice(index, 1);
          }
        });
      } else if (attribute === this.targetColumn && selectedValue.length === 2) {
        // this.ns.success('Exact 2 filters are selected');
        this.dataToServer[tabName][attribute] = [];
        selectedValue.forEach((element, index) => {
          const x = {};
          x[element] = customValue || 'True';
          this.dataToServer[tabName][attribute][index] = x;
        });
        // SMOTE
        if (dataModification.hasOwnProperty('ColumnBinning') && this.multiclassFlag === false) {
          // this.preProcessData.DataModification['ColumnBinning'][this.targetColumn];
          // this.preProcessData['Filters'][this.targetColumn];
          const colBinning = dataModification['ColumnBinning'][this.targetColumn];
          const smoteValue = [];

          Object.keys(colBinning).forEach((element, index) => {
            if (colBinning[element].hasOwnProperty('SubCatName')) {
              selectedValue.forEach((selVal, index) => {
                if (selVal === colBinning[element]['SubCatName']) {
                  smoteValue.push(colBinning[element]['Value']);
                }
              });
            }
          });
          this.smoteCalc(smoteValue);
        }
      } else if (attribute === this.targetColumn && selectedValue.length > 2) {
        this.dataToServer[tabName][attribute] = [];
        selectedValue.forEach((element, index) => {
          const x = {};
          x[element] = customValue || 'True';
          this.dataToServer[tabName][attribute][index] = x;
        });
        this.isSmoteVisible = false;
        this.dataToServer['Smote']['Flag'] = 'False';
      } else {
        this.dataToServer[tabName][attribute] = [];
        selectedValue.forEach((element, index) => {
          const x = {};
          x[element] = customValue || 'True';
          this.dataToServer[tabName][attribute][index] = x;
        });
      }
    } else if (tabName === 'DataEncoding') {

      // for (const t in this.dataEncoding) {
      //   if (this.dataEncoding.hasOwnProperty(t)) {
      //     if (this.autobinningcolsname.hasOwnProperty(t)) {
      //       this.dataEncoding[t]['ChangeRequest'] = 'False';
      //     }
      //   }
      // }
      if (selectedValue === 'Please Select An Option') {
        this.dataToServer[tabName][attribute]['encoding'] = '';

      } else {

        this.dataToServer[tabName][attribute]['encoding'] = selectedValue;
      }

    } else {
      if (selectedValue === 'Please Select An Option') {
        this.dataToServer[tabName][attribute] = {};
      } else if (selectedValue === 'CustomFlag') {
        this.dataToServer[tabName][attribute][selectedValue] = customValue;
      } else {
        this.dataToServer[tabName][attribute] = {};
        this.dataToServer[tabName][attribute][selectedValue] = customValue || 'True';
        // console.log(this.dataToServer[tabName][attribute][selectedValue]);
      }
    }
    this.isNextDisabled.emit(true);
  }

  smoteCalc(smoteValue) {
    console.log(smoteValue);
    const min = Math.min(smoteValue[0], smoteValue[1]);
    const max = Math.max(smoteValue[0], smoteValue[1]);
    const minVal = min / 10;
    const maxVal = max / 10;
    const addedVal = minVal + maxVal;
    const minRatio = +(minVal * 100 / addedVal).toFixed();
    const maxRatio = +(maxVal * 100 / addedVal).toFixed();

    if (maxRatio >= 80 && minRatio <= 20) {
      // this.preProcessData['Smote']['Flag'] = "True";
      // this.dataToServer['Smote']['Flag'] = "True";
      this.isSmoteVisible = true;
      this.smoteFlag = false;
      setTimeout(() => {
        this.smoteElement.nativeElement.checked = false;
      }, 1000);
      // this.smoteElement.nativeElement.checked = false;
    } else {
      this.isSmoteVisible = false;
      this.smoteFlag = false;
      setTimeout(() => {
        this.smoteElement.nativeElement.checked = false;
      }, 1000);
      // this.smoteElement.nativeElement.checked = false;
    }

  }

  onSmoteChecked(elementRef) {
    if (elementRef.checked === true) {
      this.dataToServer['Smote']['Flag'] = 'True';
    } else {
      this.dataToServer['Smote']['Flag'] = 'False';
    }
  }

  onBinClick() {

  }

  onPrescriptionChange(elementRef) {
    if (elementRef.checked === true) {
      this.dataTobeFixed.push(elementRef.value);
    }

    if (elementRef.checked === false) {
      const index = this.dataTobeFixed.findIndex(data => data === elementRef.value);
      this.dataTobeFixed.splice(index);
    }
  }

  // 704405 - Ingrain_StageTest_R2.1 - Sprint 2 - [Suggestion]:
  // There should be some message shown to user to click Apply button in Data Transformation page to proceed
  // Fix : Change message "Data saved successfully." to "Data saved successfully. Please click on Apply button to proceed."
  onSave() {
    if (!this.readOnly) {
      if (this.datedInputOnly) {
        this.ns.warning(`Detected Datetime Influencers only :
       Derive more influencers using ADD FEATURE or add more potential influencers in Problem Statement Page`);
        return 0;
      }
      let attributeEmpty = false;
      let targetColsEmpty = false;
      for (const key in this.dataToServer["Filters"]) {
        if (key != this.targetColumn && this.dataToServer["Filters"][key].length == 0) {
          attributeEmpty = true;
        }
        // BugFix for 995519
        if (key == this.targetColumn && this.dataToServer["Filters"][key].length < 2) {
          targetColsEmpty = true;
        }
      }
      if (!attributeEmpty && !targetColsEmpty) {

        this.setTextDataPreprocessing();
        if (this.modelType === 'Classification' && this.multiclassFlag === false) {
          this.setDataforAutobinning();
        }
        this.des.postPreProcessData(this.dataToServer).subscribe(
          data => {
            if (data === 'Success') {
              this.des.setApplyFlag(true);
              this.isNextDisabled.emit(true);
              this.ns.success('Data saved successfully. Please click on Apply button to proceed.');
            }
          }
        );
      } else if (attributeEmpty) {
        this.ns.error('Please select the classes for the selected Attribute under Filter section');
      } else if (targetColsEmpty) {
        this.ns.error('Please select atleast 2 filters for target column');
      }
    }
  }


  checkRetrainStatus() {
    //  const isdata = this.ps.getColumnsdata.IsModelTrained;
    if (this.IsModelTrained !== undefined) {
      if (this.IsModelTrained === true) {
        this.modalRef = this._modalService.show(this.confirmChangesOnDataTransformation, this.config2);
      } else {
        this.onApply();
      }
    } else {
      this.onApply();
    }
  }

  confirm() {
    this.modalRef.hide();
    this.onApply();
  }
  // confirmation popup cancle event
  decline() {
    this.modalRef.hide();
  }

  onApply(retryApply?) {

    if (this.datedInputOnly) {
      this.ns.warning(`Detected Datetime Influencers only :
         Derive more influencers using ADD FEATURE or add more potential influencers in Problem Statement Page`);
      return 0;
    }

    // BugFix for 865051
    let attributeEmpty = false;
    let targetColsEmpty = false;
    if (!retryApply) {
      for (const key in this.dataToServer["Filters"]) {
        if (key != this.targetColumn && this.dataToServer["Filters"][key].length == 0) {
          attributeEmpty = true;
        }
        // BugFix for 995519
        if (key == this.targetColumn && this.dataToServer["Filters"][key].length < 2) {
          targetColsEmpty = true;
        }
      }
    }

    if (!attributeEmpty && !targetColsEmpty) {
      this.isLoading = true;
      this.userID = this.aus.getCookies().UserId;
      for (const key in this.defaultFilterOptions) {
        if (this.defaultFilterOptions[key].length > 0 && this.dataToServer["Filters"][key] == undefined) {
          this.dataToServer['Filters'][key] = [];
        }
      }
      if (this.modelType === 'Classification' || this.modelType === 'Multi_Class' || this.modelType === 'Regression') {
        this.setTextDataPreprocessing();
      }

      if (this.modelType === 'Classification' && this.multiclassFlag === false) {
        this.setDataforAutobinning();
      }
      // if (Object.keys(this.dataToServer.MissingValues).length === this.missingValueAttributes.length) {
      this.applySubscription = this.des.apply(this.correlationId, this.userID, 'DataPreprocessing', this.dataToServer).pipe(

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
              return this.des.apply(this.correlationId, this.userID, 'DataPreprocessing', this.dataToServer);
            } else if (tempData.hasOwnProperty('PreprocessedData')) {
              return of(tempData);
            } else {
              this.noData = 'Format from server is not Recognised';
              this.ns.error(`Error occurred:
                Due to some backend data process the relevant data could not be produced.
                Please try again while we troubleshoot the error`);
              return EMPTY;
            }

          }
        ),
        catchError(error => {
          return EMPTY;
        })

      ).subscribe(data => {
        this.pythonProgressStatus = data['Status'];
        this.pythonProgress = data['Progress'];
        if (this.pythonProgressStatus === 'P') {
          this.isLoading = true;
          this.pythonProgress = data['Progress'];
          this.retry();
        } else if (this.pythonProgressStatus === 'C') {
          this.dataToServer['SmoteMulticlass'] = {};
          this.nextDisabled = false;
          this.ns.success('Data saved successfully.');
          this.preProcessData = Object.assign({}, data.PreprocessedData);
          this.missingValues = [];
          this.missingValueAttributes = [];
          this.dataEncodingAttributes = [];
          this.filterAttributes = [];
          this.dataModificationAttributes = [];
          this.bininngDataAttributes = [];
          /* if (this.dataToServer.DataModification.hasOwnProperty('NewAddFeatures')) {
            delete this.dataToServer.DataModification['NewAddFeatures'];
          } */
          this.filterAttributes = [];
          if (data.FeatureSelectionData && data.FeatureSelectionData.hasOwnProperty('Features_Created')
            && data.FeatureSelectionData.hasOwnProperty('Feature_Not_Created')) {
            if (data.FeatureSelectionData.Features_Created.length > 0) {
              this.ns.success('Feature created successfully ' + data.FeatureSelectionData.Features_Created.join(' '));
            }
            let notcreated = data.FeatureSelectionData.Feature_Not_Created;
            if (Object.keys(notcreated).length !== 0) {
              notcreated = JSON.stringify(notcreated);
              const message = notcreated.substring(2, notcreated.length - 2);
              this.ns.error('Feature not created ' + message);
            }
          }
          this.setData(data);
          this.unsubscribe();
          this.isLoading = false;
          if (data.hasOwnProperty('DataPointsWarning') && data.DataPointsWarning !== null) {
            this.ns.warning(data.DataPointsWarning);
          }
        } else if (data['Status'] === 'E') {
          this.ns.error(data['Message']);
          this.preProcessData = {};
          this.filters = {};
          this.missingValues = [];
          this.dataEncodingAttributes = [];
          this.ngOnInit();
          this.isLoading = false;
          this.nextDisabled = false;
        } else {
          this.noData = 'Format from server is not Recognised';
          this.ns.error(`Error occurred: Due to some backend data process
           the relevant data could not be produced. Please try again while we troubleshoot the error`);
          this.unsubscribe();
          this.isLoading = false;
        }

      }
      );

      // if (this.modelType === 'Classification') {
      //   this.binningFlag = true;
      // }
      // }
      // else {
      //   this.isLoading = false;
      //   this.ns.error('Please select all the missing values to proceed');
      // }
    } else if (attributeEmpty) {
      this.ns.error('Please select the classes for the selected Attribute under Filter section');
    } else if (targetColsEmpty) {
      this.ns.error('Please select atleast 2 filters for target column');
    }
  }

  unsubscribe() {
    if (!this.cus.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
    if (!this.cus.isNil(this.applySubscription)) {
      this.applySubscription.unsubscribe();
    }
    this.isLoading = false;
  }

  retry() {
    this.timerSubscripton = timer(10000).subscribe(() => this.onApply(true));
    return this.timerSubscripton;
  }

  //   setEncodingPayload() {
  //   for (const t in this.dataEncoding) {
  //     if (this.dataEncoding.hasOwnProperty(t)) {
  //       if (this.autobinningcolsname.hasOwnProperty(t)) {
  //         this.dataEncoding[t]['ChangeRequest'] = 'False';
  //       }
  //     }
  //   }
  // }

  onSaveAs($event) {
    const openTemplateAfterClosed = this.dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this.des.clone(this.correlationId, modelName);
        } else {
          return EMPTY;
        }
      }),

      tap(data => this.newCorrelationIdAfterCloned = data),

      tap(data => {

        if (data) {
          this.router.navigate(['dashboard/dataengineering/preprocessdata'], {
            queryParams: { modelName: this.newModelName, correlationId: data[0] }
          });

          this.ns.success('Cloned Successfully');
        }
      })
    );

    openTemplateAfterClosed.subscribe();
  }

  IsJsonString(str) {
    try {
      JSON.parse(str);
    } catch (e) {
      return false;
    }
    return true;
  }

  openAddFeatureTypeSelection(addFeatureManualModal: TemplateRef<any>) {
    this.uts.usageTracking('Data Engineering', 'Add Feature');
    // this.modalRefForType = this._modalService.show(addFeatureSelectionPopUP, { class: 'preprocess-addfeature' });
    this.modalRefForManual = this._modalService.show(addFeatureManualModal, this.config);
  }

  onChecked(elementRef, addFeatureManualModal) {
    const value = elementRef.value;
    console.log(value);
    if (value === 'Manual') {
      this.modalRefForType.hide();
      this.modalRefForManual = this._modalService.show(addFeatureManualModal, this.config);
      // this.modalRefForManual.setClass('preprocess-addfeature-manualmodal');
    }
  }

  // Missing Values Component
  setDatatoserverForMissingValuesOnChange(data) {
    this.setDatatoserver(data[0], data[1], data[2], data[3]);
  }

  setDatatoserverForMissingValueObject(data) {
    this.dataToServer.MissingValues = data;
  }
  // Missing Values Component

  // Fliter Component
  setDefaultFilterOptions(data) {
    this.defaultFilterOptions = data;
  }

  setDatatoserverFilterValueObject(data) {
    this.dataToServer.Filters = data;
  }

  setDatatoserverFilterValuesOnChange(data) {
    if (data[2] === 'null') {
      this.RemoveFilterColumn = data[1];
      // this.Removekeys.push(data[1]);
      if (this.filters !== undefined && this.filters !== '{}') {
        const filterkeys = Object.keys(this.filters);
        for (let i = 0; i < filterkeys.length; i++) {
          if (this.RemoveFilterColumn === filterkeys[i]) {
            // this.dataToServer['Filters'][this.RemoveFilterColumn] = [];
            delete this.dataToServer['Filters'][this.RemoveFilterColumn];
            // Smote Changes for Target Column
            if (filterkeys[i] === this.targetColumn) {
              this.isSmoteVisible = false;
              this.dataToServer['Smote']['Flag'] = 'False';
            }
          }
        }
      }
    } else {
      this.setDatatoserver(data[0], data[1], data[2], data[3]);
    }
  }

  setDatatoserverDataModification(data) {
    this.dataToServer.DataModification = data;
  }

  setDatatoserverDataModificationOnChange(data) {
    this.setDatatoserver(data[0], data[1], data[2], data[3]);
  }

  setDatatoserverDataEncodingOnChange(data) {
    this.setDatatoserver(data[0], data[1], data[2], data[3]);
  }


  // Save TextDataPreprocessing

  setTextDataPreprocessing() {
    let DeletedTextColumnByUser = [];
    let TextDataPreprocessing = {};
    if (this.des.deleteByUser) {
      const deletedByUserVar = Object.keys(this.des.deleteByUser);
      TextDataPreprocessing = Object.assign({}, this.des.TextDataPreprocessing);
      DeletedTextColumnByUser = [];
      if (deletedByUserVar && deletedByUserVar.length > 0) {
        deletedByUserVar.forEach((element) => {
          if (this.des.deleteByUser[element] === 'true') {
            DeletedTextColumnByUser.push(element);
            delete TextDataPreprocessing[element];
          }
        });
      }
    }
    this.dataToServer.DataModification['TextDataPreprocessing'] = TextDataPreprocessing;
    this.dataToServer.DataModification['TextDataPreprocessing']['TextColumnsDeletedByUser'] = DeletedTextColumnByUser;
  }

  setDatatoserverforbinning(colsname) {
    if (this.multiclassFlag === false) {
      this.autobinningcolsname = colsname;
      this.isBinningFlag = true;
      this.dataencoding.getAutoBinningColumns(colsname, this.categoricalcolumns);
    }
  }

  onCheckedSmote(elementRef) {
    let objsmotemulticlass;
    if (elementRef.checked === true) {
      objsmotemulticlass = {
        'UserConsent': 'True',
        'ChangeRequest': 'True',
        'PChangeRequest': 'False'
      };
    } else {
      objsmotemulticlass = {
        'UserConsent': 'False',
        'ChangeRequest': 'False',
        'PChangeRequest': 'False'
      };
    }
    this.dataToServer['SmoteMulticlass'] = objsmotemulticlass;
  }


  setDataforAutobinning() {
    if (this.isBinningFlag === false) {
      this.BinningColumns = this.autoBinning;
      for (const t in this.BinningColumns) {
        if (this.BinningColumns.hasOwnProperty(t)) {
          if (this.autobinningcolsname.hasOwnProperty(t)) {
            this.BinningColumns[t] = 'True';
          }
        }
      }
    } else {
      this.BinningColumns = this.autoBinning;
      for (const t in this.BinningColumns) {
        if (this.BinningColumns.hasOwnProperty(t)) {
          if (this.autobinningcolsname.hasOwnProperty(t)) {
            this.BinningColumns[t] = 'True';
          } else {
            this.BinningColumns[t] = 'False';
          }
        }
      }
    }
    this.dataToServer.DataModification['AutoBinning'] = this.BinningColumns;
  }

  // this.applyFlag

  private disableTabs() {
    // this.ps.getColumnsdata.IsModelTrained
    const classnameDisabled = 'anchor-disabled';
    const classnameEnabled = 'anchor-enabled';
    const nativeElemnt = !this.cus.isNil(this.eleRef.nativeElement) ? this.eleRef.nativeElement : null;
    const parentEle = !this.cus.isNil(nativeElemnt) ? nativeElemnt.parentElement : null;
    const nestedParentEle = !this.cus.isNil(parentEle) ? parentEle.parentElement : null;
    /* if (!this.cus.isNil(nestedParentEle)) {
      const allLinks = nestedParentEle.children[0].querySelectorAll('a');
      const submenulinks = nestedParentEle.children[2].querySelectorAll('a');
      for (let index = 0; index < allLinks.length; index++) {
        if (allLinks[index].text === 'Model Engineering') {
          if (this.applyFlag) {
            allLinks[index].className = classnameEnabled;
          } else {
            allLinks[index].className = classnameDisabled;
          }
        }
      }
    } */
  }

  next() {
    if (this.datedInputOnly) {
      this.ns.warning(`Detected Datetime Influencers only :
       Derive more influencers using ADD FEATURE or add more potential influencers in Problem Statement Page`);
      return 0;
    }
    this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, null, true);
    this.customRouter.redirectToNext();
  }

  confirmPopup() {

  }

  previous() {
    if (this.customRouter.urlAfterRedirects === '/dashboard/dataengineering/datacleanup'
      || this.router.url === '/dashboard/dataengineering/datacleanup') {
      const modelname = this.ls.getModelName();
      const modelCategory = this.ls.getModelCategory();
      let requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelName=' + modelname + '&pageSource=true';
      if (modelCategory !== null && modelCategory !== undefined && modelCategory !== '') {
        requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelCategory=' + modelCategory +
          '&displayUploadandDataSourceBlock=true&modelName=' + modelname;
      }
      this.customRouter.previousUrl = requiredUrl;
    }
    this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex, true, null);
    this.customRouter.redirectToPrevious();
  }


  expandAll() {
    this.accordian = {
      'acc01': false,
      'acc02': false,
      'acc03': false,
      'acc04': false,
      'acc05': false,
      'expandAll': false
    }
  }

  collapseAll() {
    this.accordian = {
      'acc01': true,
      'acc02': true,
      'acc03': true,
      'acc04': true,
      'acc05': true,
      'expandAll': true
    }
  }
}
