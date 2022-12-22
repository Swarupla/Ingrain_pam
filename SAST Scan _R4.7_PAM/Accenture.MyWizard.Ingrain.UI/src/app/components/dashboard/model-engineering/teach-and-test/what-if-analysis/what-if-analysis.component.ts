
import {
    Component, OnInit, Input, OnChanges, ViewChild, ElementRef, Inject, ChangeDetectorRef,
    AfterContentChecked, ChangeDetectionStrategy,
    HostListener,
    TemplateRef
} from '@angular/core';
import { RegressionPopupComponent } from '../../../problem-statement/regression-popup/regression-popup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, switchMap, catchError } from 'rxjs/operators';
import { SaveScenarioPopupComponent } from '../save-scenario-popup/save-scenario-popup.component';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { empty, throwError, timer, of, pipe, EMPTY } from 'rxjs';
import { LocalStorageService } from '../../../../../_services/local-storage.service';
import { TeachTestService } from '../../../../../_services/teach-test.service';
import { FileUploadProgressBarComponent } from '../../../../file-upload-progress-bar/file-upload-progress-bar.component';
import { CookieService } from 'ngx-cookie-service';
import { CoreUtilsService } from '../../../../../_services/core-utils.service';
import { AppUtilsService } from '../../../../../_services/app-utils.service';
import { Router, ActivatedRoute } from '@angular/router';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { max } from 'moment';
import { ConfirmationPopUpComponent } from '../../../data-engineering/preprocess-data/confirmation-pop-up/confirmation-popup.component';
import { ExcelService } from 'src/app/_services/excel.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { DomSanitizer } from '@angular/platform-browser';
// import { forEach } from '@angular/router/src/utils/collection';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { element } from 'protractor';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { NotificationData } from 'src/app/_services/usernotification';
import * as $ from 'jquery';
import { truncate } from 'node:fs/promises';
declare var userNotification: any;

@Component({
    selector: 'app-what-if-analysis',
    templateUrl: './what-if-analysis.component.html',
    styleUrls: ['./what-if-analysis.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class WhatIfAnalysisComponent implements OnInit, OnChanges, AfterContentChecked {

    @Input() selectedModelName: any;
    @ViewChild('fileInput', { static: false }) fileInput: any;
    @ViewChild('PrescriptiveToogle', { static: false }) PrescriptiveToogle: any;
    displayDataSourceLink: boolean;
    correlationId: string;
    showSaveAs: boolean;
    testCaseScenarioOptions;
    fileName: string;
    dataSourceName: string;
    files = [];
    teachAndTestData: any;
    teachTestList: any;
    strOptions: any = { key: '', value: '' };
    hyperTunning: boolean;
    chartData: Array<any>;
    minValue: any;
    maxValue: any;
    isFloat: any;
    isString: any;
    stringOptions: any = [];
    featureType: any = [];
    minNumberArr: any = [];
    maxNumberArr: any = [];
    freequencyOptions: any;
    optionvalue: string;
    rangeValue: number;
    showRunBulkTestbutton: boolean;
    postFeatureValues: any = {};
    postFeatureList: any = [];
    postData: any = { Name: '', Selection: '', Value: 0 };
    wfId: string;
    modelName: string;
    useCase: string;
    testCaseScenarioName: string;
    multipleFeaturesList;
    page: 1;
    totalRec: number;
    pageInfo: string;
    showButtons: boolean;
    uploadedData: any;
    predictionDataCount: number;
    testDataListCount: number;
    predictionData: any;
    predictionDataList: any = [];
    testDataList;
    runTestData: any;
    barChartData: any;
    disableSavetest: boolean;
    disableSavebtn: boolean;
    newModelName: string;
    newCorrelationIdAfterCloned: string;
    isSelectionChange: boolean;
    freequecnyTypeonSelection: string;
    disableForTestCase: boolean;
    statusForPrescription = true;
    targetColumn: any;
    showRunTestbutton: boolean;
    frequency: string;
    modelType: string;
    disableForTimeSeries: boolean;
    showRunTestbuttonTimeSeries: boolean;
    disableSavetestTimeSeries: boolean;
    lineChartDataCount: number;
    stepsValue: number;
    freequecnyType: string;
    selectedModelLineChart: any[];
    lineChart: any[];
    objLineChart: {} = {};
    isData: boolean;
    stepsValueonSelection: any;
    showSaveTestTimeSeries: boolean;
    screenWidth: any;
    isTextPreProcessing = false;
    isNLPClassification: boolean;
    tableDataforDownload = [];
    allowedExtensions = ['.xlsx', '.csv'];
    predictionOutcomeValues = [];
    prescriptivevalues = [];
    predictionOutcome: any;
    config = {
        backdrop: true,
        ignoreBackdropClick: true,
        class: 'deploymodle-confirmpopup'
    };
    modalRef: BsModalRef | null;
    disablePrescriptiveButton = false;
    disableReset = false;
    prescriptiveAnalysis = false;
    presAnalysisData = [];
    prescriptiveDonutValue;

    prescriptive_desired_value;
    runtestSubscription: any;
    timerSubscription: any;
    runtestSubscriptionTimeSeries: any;
    timerSubscriptionTimeSeries: any;
    featureWeightsArray;
    isLoading = false;
    openFeatureValues = true;

    featureChartsHWClassification = {
        height: 315, // 300,
        width: 470
    };

    /* featureChartsHWRegression = {
      height: 280,
      width:  280
    }; */

    PredictionOutcomeChart = {
        height: 200,
        width: 470
    };

    isRunTestorRunBulkTest = 'False';
    disbalePrescriptiveAnalysisTextColumns = false;
    showWordCloud = false;

    clusteringFlag: string = 'true';
    hyperDisableForTimeSeries;
    pASelected = false;
    disablePAToggle = false;
    PAInput = true;
    predictiveOutcome = [];
    isNLPFlag;
    runTestDatacpoy: any;
    decimalPoint: any;
    instanceType: string;
    env: string;
    // PIConfirmation = false;

    constructor(@Inject(ElementRef) private eleRef: ElementRef, private notificationService: NotificationService,
        private localStorService: LocalStorageService, private teachTestService: TeachTestService,
        private dialogService: DialogService, private changeDetectorRef: ChangeDetectorRef,
        private cookieService: CookieService, private coreUtilsService: CoreUtilsService,
        private appUtilsService: AppUtilsService, private ps: ProblemStatementService,
        private _router: Router, private _problemStatementService: ProblemStatementService, private _excelService: ExcelService,
        private _modalService: BsModalService, private ns: NotificationService,
        private domSanitizer: DomSanitizer, private _usageTrackingService: UsageTrackingService) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
    }

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        this.env = sessionStorage.getItem('Environment');
        this.screenWidth = window.innerWidth;
        this.multipleFeaturesList = [];
        this.predictionDataList = [];
        this.disableSavetest = true;
        this.disableSavebtn = true;
        this.disableForTestCase = true;
        this.showRunTestbutton = false;
        this.testCaseScenarioName = '';
        this.predictionDataCount = 0;
        this.testDataListCount = 0;
        this.page = 1;
        const _this = this;
        this.pageInfo = 'WFIngestData';
        this.selectedModelName = localStorage.getItem('SelectedRecommendedModel'); // this.cookieService.get('SelectedRecommendedModel');
        this.frequency = this.cookieService.get('Frequency');
        this.modelType = this.cookieService.get('ProblemType');
        this.correlationId = this.localStorService.getCorrelationId();

        this.appUtilsService.loadingStarted();
        this.isSelectionChange = true;
        this.disableForTimeSeries = false;
        this.showRunTestbuttonTimeSeries = true;
        this.disableSavetestTimeSeries = true;
        this.lineChartDataCount = 0;
        this.stepsValue = 0;
        this.disablePrescriptiveButton = false;
        this.disablePAToggle = false;
        this.disableReset = true;

        this.teachTestService.getTeachTestData(this.correlationId, this.modelType, this.frequency, this.selectedModelName).subscribe(data => {
            this.cookieService.set('ModelTypeForInstaML', data.ModelType);
            this.isNLPFlag = data['NLP_Flag'];
            this.modelType = data.ModelType;
            this.predictionOutcomeValues = data.TargetColUniqueValues;
            // this.prescriptivevalues = data.TargetColUniqueValues;
            this.clusteringFlag = this.localStorService.getClusteringFlag();
            if (this.modelType === 'TimeSeries') {
                this.hyperDisableForTimeSeries = true;
                this.setTimeSeriesData(data);
            } else {
                this.setFeatureList(_this, data, true);
            }
            this.appUtilsService.loadingEnded();
        }, error => {
            this.appUtilsService.loadingEnded();
            this.notificationService.error('Something went wrong to getTeachTestData.');
        }
        );
        this.displayDataSourceLink = true;
        this.targetColumn = this.ps.getTargetColumn();

    }
    ngOnChanges() {
        this.screenWidth = window.innerWidth;
    }
    setTimeSeriesData(data) {
        this.disableForTimeSeries = true;
        this.showRunTestbuttonTimeSeries = true;
        this.disableSavetestTimeSeries = true;
        this.isSelectionChange = true;
        this.showButtons = true;
        const _this = this;
        if (!this.coreUtilsService.isNil(data.ModelType) && data.ModelType === 'TimeSeries'
            && !this.coreUtilsService.isNil(data.TeachtestData)) {
            if (!this.coreUtilsService.isNil(data.TeachtestData.TimeSeriesParams.steps)) {
                const teachtestData = data.TeachtestData;
                if (_this.coreUtilsService.isIE()) {
                    Object.entries = undefined;
                    if (!Object.entries) {
                        // tslint:disable-next-line: no-shadowed-variable
                        Object.entries = function (teachtestData) {
                            const ownProps = Object.keys(teachtestData);
                            let i = ownProps.length;
                            const resArray = new Array(i);
                            while (i--) {
                                resArray[i] = [ownProps[i], teachtestData[ownProps[i]]];
                            }
                            return resArray;
                        };
                    }
                }
                this.freequencyOptions = [];
                Object.entries(teachtestData.TimeSeriesParams.steps).forEach(
                    ([key, value]) => {
                        _this.freequecnyType = key;
                        const values = [];
                        values.push(value);
                        const stepData = values[0];
                        for (let index = 0; index < stepData.length; index++) {
                            _this.strOptions = { key: '', value: '' };
                            _this.strOptions.key = stepData[index];
                            _this.strOptions.value = stepData[index];
                            _this.freequencyOptions.push(_this.strOptions);
                        }
                    });
            }
        }
        if (!_this.coreUtilsService.isNil(data.TeachtestModelData)) {
            this.testCaseScenarioOptions = data.TeachtestModelData;
        }
    }
    setFeatureList(_this: any, data: any, isAutoRun) {
        _this.setTeachAndTestData(data);
        // this.appUtilsService.loadingEnded();
        _this.multipleFeaturesList = [];
        if (!_this.coreUtilsService.isNil(data.TeachtestData)) {
            if (data.hasOwnProperty('FeatureImportance')) {
                this.featureWeightsArray = data.FeatureImportance;
                const featureData = this.featureWeightsArray.filter(x => x.modelName === this.selectedModelName);
                if (featureData.length > 0) {
                    const sortedData = Object.entries(featureData[0].featureImportance).sort(function (a: any, b: any) { return b[1] - a[1]; });
                    const newObj = {};
                    for (let index = 0; index < sortedData.length; index++) {
                        const key = sortedData[index][0];
                        const value = data.TeachtestData[key];
                        newObj[key] = value;
                    }
                    this.multipleFeaturesList.push(newObj);
                } else {
                    this.multipleFeaturesList.push(data.TeachtestData);
                }

            } else {
                this.multipleFeaturesList.push(data.TeachtestData);
            }
            this.totalRec = this.multipleFeaturesList.length;
            for (let index = 0; index < this.multipleFeaturesList.length; index++) {
                _this.teachTestList = this.multipleFeaturesList[index];
                _this.setFeatureData(_this, _this.teachTestList, index, isAutoRun);
            }
        }
        if (!_this.coreUtilsService.isNil(data.TeachtestModelData)) {
            this.testCaseScenarioOptions = data.TeachtestModelData;
        }
        _this.showButtons = true;
        // To Test Sample Word Cloud
        // this.onTestScenarionChange('b9836104-f729-4b25-bcb9-12948c251ad4', '');
    }

    setFeatureData(_this: any, teachTestList: any, index: any, isAutoRun) {
        _this.disbalePrescriptiveAnalysisTextColumns = false;
        if (_this.coreUtilsService.isIE()) {
            Object.entries = undefined;
            if (!Object.entries) {
                // tslint:disable-next-line: no-shadowed-variable
                Object.entries = function (teachTestList) {
                    const ownProps = Object.keys(teachTestList);
                    let i = ownProps.length;
                    const resArray = new Array(i);
                    while (i--) {
                        resArray[i] = [ownProps[i], teachTestList[ownProps[i]]];
                    }
                    return resArray;
                };
            }
        }
        Object.entries(teachTestList).forEach(
            ([key, value]) => {
                _this.postData = { Name: '', Selection: '', Value: 0 };
                let miniValue;
                let maxValue;
                let strValue = '';
                let textDataType: any = false;
                let textValue = '';
                Object.entries(value).forEach(
                    // tslint:disable-next-line: no-shadowed-variable
                    ([key, value]) => {
                        if (key.indexOf('Min int Value') > -1) {
                            const splitValues = key.split(':');
                            miniValue = Number(splitValues[1].replace(/\s/g, ''));
                        }
                        if (key.indexOf('Max int Value') > -1) {
                            const splitValues = key.split(':');
                            maxValue = Number(splitValues[1].replace(/\s/g, ''));
                        }
                        if (key.indexOf('Min float Value') > -1) {
                            const splitValues = key.split(':');
                            miniValue = parseFloat(splitValues[1].replace(/\s/g, ''));
                        }
                        if (key.indexOf('Max float Value') > -1) {
                            const splitValues = key.split(':');
                            maxValue = parseFloat(splitValues[1].replace(/\s/g, ''));
                        }
                        if (key.indexOf('textbox') > -1) {
                            const splitValues = key.split(':');
                            textValue = splitValues[1];
                            textDataType = true;
                        }
                        if (this.coreUtilsService.isNil(miniValue) && this.coreUtilsService.isNil(maxValue) && textDataType === false) {
                            strValue = this.coreUtilsService.isNil(strValue) ? key : strValue;
                        }
                    });
                if (miniValue || maxValue || miniValue === 0 || maxValue === 0) {
                    // _this.postData.Value = miniValue;
                    _this.postData.Value = this.randomInitialValues(miniValue, maxValue);
                    _this.postData.Type = 'float';
                    _this.getTypeValue(key, value, 'float', miniValue, maxValue);
                } else if (textDataType === true) {
                    //  _this.disbalePrescriptiveAnalysisTextColumns = true;
                    //  _this.showRunTestbutton = true;
                    _this.postData.Value = textValue;
                    _this.postData.Type = 'text';
                    _this.getTypeValue(key, value, 'text', miniValue, maxValue);
                } else {
                    _this.postData.Value = strValue;
                    _this.postData.Type = 'string';
                    _this.getTypeValue(key, value, 'string', miniValue, maxValue);
                }
                _this.postData.Selection = 'True';
                _this.postData.Name = key;
                _this.postFeatureValues[key] = _this.postData;
            }
        );
        _this.postFeatureList[index] = _this.postFeatureValues;
        if (isAutoRun === true) {
            this.runTest();
        } else {
            // Deferred Defect
            // this.predictionDataList = [];
            // this.testDataList = [];
            // Bug 715322 Ingrain_StageTest_R2.1 - Sprint 2 - Teach and Test - After saving bulk test its displaying Pagination with a blank page
            // Fix :- reset count to zero

            this.predictionDataCount = 0;
            this.testDataListCount = 0;
        }
    }

    ngAfterContentChecked(): void {
        this.changeDetectorRef.detectChanges();
    }

    setTeachAndTestData(teachAndTestData: any) {
        this.modelName = teachAndTestData.ModelName;
        this.dataSourceName = teachAndTestData.DataSource;
        this.useCase = teachAndTestData.BusinessProblem;
    }

    onSelectChange(listIndex: any, key: string, value: any, index: any, elementRef: any) {
        this.postFeatureList[listIndex][key].Value = value;
        this.disableSavetest = true;
        this.disableSavebtn = true;
    }
    onInputChange(listIndex: any, key: string, value: any, inputRange) {
        this.postFeatureList[listIndex][key].Value = parseFloat(value);
        this.disableSavetest = true;
        this.disableSavebtn = true;
    }

    // tslint:disable-next-line: max-line-length
    // Bug 708123 Ingrain_StageTest_R2.1 - Sprint 2 - When we enter space or special characters in the text column in Teach and test screen its showing both "Run Test completed and Error message"
    // Fix added regex for checking multiple space isOnlySpaces and trimmed value if contains some text

    onTextChange(listIndex: any, key: string, value: any, textInput) {
        if (value === '') {
            this.ns.warning('Text box can not be empty. Kindly enter a value.');
        }
        this.postFeatureList[listIndex][key].Value = value;
        this.disableSavetest = true;
        this.disableSavebtn = true;
    }

    onCheckChanged(listIndex: any, key: string, elementRef: any) {
        this.showRunTestbutton = false;
        if (elementRef.checked === true) {
            this.postFeatureList[listIndex][key].Selection = 'True';
        } else if (elementRef.checked === false) {
            this.postFeatureList[listIndex][key].Selection = 'False';
        }
        const allvalues = Object.values(this.postFeatureList[listIndex]);
        const allChecked = allvalues.filter(value => value['Selection'] === 'False');
        if (allvalues.length === allChecked.length) {
            this.showRunTestbutton = true;
        }
    }
    openRegressionPopup() {
        const openFileProgressAfterClosed = this.dialogService.open(RegressionPopupComponent,
            { data: {} }).afterClosed.pipe(
                tap(data => '')
            );
        openFileProgressAfterClosed.subscribe();
    }
    // getFileDetails(event) {
    //   this.files = [];
    //   for (let i = 0; i < event.target.files.length; i++) {
    //     this.files.push(event.target.files[i]);
    //   }
    //   this.openFileProgressDialog(this.files, this.correlationId);
    // }

    getFileDetails(e) {
        this.files = [];
        let validFileExtensionFlag = true;
        let validFileNameFlag = true;
        let validFileSize = true;
        const files = e.target.files;
        for (let i = 0; i < e.target.files.length; i++) {
            const fileName = files[i].name;
            const dots = fileName.split('.');
            const fileType = '.' + dots[dots.length - 1];
            if (!fileName) {
                validFileNameFlag = false;
                break;
            }
            if (this.allowedExtensions.indexOf(fileType) !== -1) {
                if (e.target.files[i].size <= 136356582) {
                    const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
                    validFileNameFlag = true;
                    validFileExtensionFlag = true;
                    validFileSize = true;
                    if (index < 0) {
                        this.files.push(e.target.files[i]);
                    }
                } else {
                    validFileSize = false;
                }
            } else {
                validFileExtensionFlag = false;
                break;
            }
        }
        if (validFileNameFlag !== false && validFileExtensionFlag !== false && validFileSize !== false) {
            this.openFileProgressDialog(this.files, this.correlationId);
        } else if (validFileNameFlag === false) {
            this.notificationService.error('Kindly upload a file with valid name.');
        }
        if (validFileExtensionFlag === false) {
            this.notificationService.error('Kindly upload .xlsx or .csv file.');
        }
        if (validFileSize === false) {
            this.notificationService.error('Kindly upload file of size less than 130MB.');
        }
        /* if (this.PIConfirmation === false) {
          this.notificationService.error('Kindly select the PI data confirmation options.');
        } */
    }

    openFileProgressDialog(filesData, correlationId) {

        const _this = this;
        this.appUtilsService.loadingStarted();
        this.fileInput.nativeElement.value = '';
        const openFileProgressAfterClosed = this.dialogService.open(FileUploadProgressBarComponent,
            { data: { filesData: filesData, correlationId: correlationId, pageInfo: this.pageInfo, isTeachAndtest: 'ture' } }).afterClosed.pipe(
                tap(data => {
                    if (data !== undefined) {
                        _this.populateFeatureAfterUpload(data);
                        _this.showRunBulkTestbutton = true;

                        _this.fileInput.nativeElement.value = '';
                        _this.appUtilsService.loadingEnded();
                    } else {
                        _this.appUtilsService.loadingEnded();
                    }
                })
            );
        openFileProgressAfterClosed.subscribe();
    }

    populateFeatureAfterUpload(uploadedData: any) {

        this.multipleFeaturesList = [];
        this.multipleFeaturesList.push(uploadedData.TeachtestData);
        this.totalRec = this.multipleFeaturesList.length;

        this.uploadedData = uploadedData;

        for (let index = 0; index < this.multipleFeaturesList.length; index++) {
            this.teachTestList = this.multipleFeaturesList[index];
            this.setFeatureData(this, this.teachTestList, index, false);
        }

        this.showButtons = true;
        this.disableSavetest = true; // After file ulpoad disable to save test
    }

    openScenarioPopup() {
        const _this = this;
        if (this.coreUtilsService.isNil(this.wfId)) {
            this.notificationService.error('Kindly do run test or run bulk test to save test case scenario');
        } else {
            const openModelScenarioPopup = this.dialogService.open(SaveScenarioPopupComponent, {}).afterClosed.pipe(
                switchMap(testScenarioName => {
                    testScenarioName ? _this.saveTestScenario(testScenarioName) : '';
                    // tslint:disable-next-line: deprecation
                    return empty();
                }),
                tap(data => {
                    if (data) {
                        this.notificationService.success('');
                    }
                })
            );
            openModelScenarioPopup.subscribe();
        }

    }

    saveTestScenario(testScenarioName: string) {
        const _this = this;
        if (this.coreUtilsService.isNil(this.wfId)) {
            this.notificationService.error('Kindly do run test or run bulk test to save test case scenario');
        } else {
            this.appUtilsService.loadingStarted();
            let scenario;
            if (this.prescriptiveAnalysis === true) {
                scenario = 'PA';
            } else {
                scenario = 'WF';
            }
            this.teachTestService.saveTestResults(testScenarioName, this.correlationId, this.wfId, scenario).subscribe(
                dataTestScenario => {
                    this.showRunTestbutton = false;
                    this.disableSavetest = true;
                    this.disableForTimeSeries = true;
                    /* if (dataTestScenario === 'Success') {
                      this.notificationService.success('Test Case Saved Succesfully');
                    }
                    this.notificationService.success('Updating Test Scenarios List');*/
                    this.teachTestService.getTeachTestData(this.correlationId, this.modelType, this.frequency, this.selectedModelName)
                        .subscribe(data => {
                            this.notificationService.success('Scenario Saved');
                            this.predictionOutcomeValues = data.TargetColUniqueValues;
                            this.disablePrescriptiveButton = false;
                            this.setFeatureList(_this, data, false);
                            // tslint:disable-next-line: max-line-length
                            // Bug 715034 Ingrain_Stage test_R2.1 -In Prescriptive Analytics  after clicking on  save it is displaying as if we have open some saved scenario
                            // Fix :- Reset the Flag to False
                            this.prescriptiveAnalysis = false;
                            this.showRunBulkTestbutton = false;
                            this.predictionDataCount = 0;
                            this.testDataListCount = 0;
                            if (data) {
                                this.appUtilsService.loadingEnded();
                            }
                        }, error => {
                            this.appUtilsService.loadingEnded();
                            this.notificationService.error('Something went wrong to saveTestScenario.');
                        }
                        );
                }, error => {
                    this.appUtilsService.loadingEnded();
                    if (error.error) {
                        this.notificationService.error(error.error);
                    } else {
                        this.notificationService.error('Something went wrong to saveTestScenario.');
                    }
                }
            );
        }
    }

    onTestScenarionChange(wfId: string, scenarioName: string) {
        this.pASelected = false;
        this.prescriptiveAnalysis = false;
        const _this = this;
        this.screenWidth = window.innerWidth;
        this.testDataList = [];
        this.disableSavetest = true;
        this.disableSavebtn = true;
        this.disablePrescriptiveButton = false;
        let scenario;
        this.collapseFeatureValues(true);
        if (!this.coreUtilsService.isNil(wfId)) {
            this.page = 1;
            this.disablePAToggle = true;
            this.appUtilsService.loadingStarted();
            let isTimeSeries = 'false';
            const dropdownValue = wfId.split('/');
            wfId = dropdownValue[0];
            scenario = dropdownValue[1];
            if (this.modelType === 'TimeSeries') {
                isTimeSeries = 'true';
            }
            this.selectedModelLineChart = [];

            // To Test Sample Word Cloud
            // this.correlationId = '1cb11b06-fffa-48cf-8e9c-35feb98f6c3e';
            // wfId = "b9836104-f729-4b25-bcb9-12948c251ad4";
            this.teachTestService.getTeachAndTestScenario(this.correlationId, wfId, isTimeSeries, scenario).subscribe(
                data => {
                    const testCaseData = data.TeachtestData;
                    if (data.ModelType === 'TimeSeries' || this.modelType === 'TimeSeries') {
                        this.selectedModelLineChart = [];
                        _this.selectedModelName = testCaseData.Model;
                        _this.stepsValueonSelection = data.steps;
                        _this.isSelectionChange = false;
                        _this.freequecnyTypeonSelection = (!this.coreUtilsService.isNil(testCaseData.Frequency) && testCaseData.Frequency === 'H')
                            ? 'Hourly' : testCaseData.Frequency;
                        for (let index = 0; index < testCaseData.Forecast.length; index++) {
                            const objLineChart = {};
                            objLineChart['Forecast'] = testCaseData.Forecast[index];
                            objLineChart['RangeTime'] = testCaseData.RangeTime[index];
                            this.selectedModelLineChart[index] = objLineChart;
                            this.isData = true;
                        }
                        this.lineChartDataCount = this.selectedModelLineChart.length;
                    } else {
                        if (!_this.coreUtilsService.isNil(testCaseData)) {
                            _this.selectedModelName = testCaseData.Model;
                            _this.isSelectionChange = false;
                            if (!_this.coreUtilsService.isNil(testCaseData.Predictions)) {
                                Object.entries(testCaseData.Predictions).forEach(
                                    ([key, value]) => {
                                        _this.testDataList.push(value);
                                    });
                                _this.testDataListCount = _this.testDataList.length;
                                for (let index = 0; index < _this.testDataList.length; index++) {

                                    if (_this.testDataList[index].FeatureWeights !== null) {
                                        const barchartdata = _this.generateBarChartData(_this.testDataList[index].FeatureWeights);
                                        _this.testDataList[index].FeatureWeights = barchartdata;
                                    }



                                    _this.testDataList[index]['ProblemType'] = this.coreUtilsService.isNil(testCaseData.ProblemType)
                                        ? '' : testCaseData.ProblemType;
                                    _this.testDataList[index]['scenario'] = testCaseData.scenario;
                                    _this.testDataList[index]['targetColumn'] = testCaseData.Target;
                                    if (_this.testDataList[index]['ProblemType'] === 'classification') {
                                        _this.testDataList[index].Probablities.Probab1 = (Math.round(_this.testDataList[index].Probablities.Probab1 * 100));
                                        _this.testDataList[index].Probablities.Probab2 = (Math.round(_this.testDataList[index].Probablities.Probab2 * 100));
                                        const data1 = {};
                                        // Setting Prediction Outcome Chart data
                                        data1[_this.testDataList[index].Prediction] = _this.testDataList[index].Probablities.Probab1;
                                        data1[_this.testDataList[index].OthPred] = _this.testDataList[index].Probablities.Probab2;

                                        const barchartdata1 = _this.generateBarChartData(data1);

                                        _this.testDataList[index]['OutcomePrediction'] = barchartdata1;
                                    }
                                    if (_this.testDataList[index]['ProblemType'] === 'regression') {
                                        _this.testDataList[index].Prediction = _this.testDataList[index].Prediction.toFixed(2);
                                        this.predictionOutcome = _this.testDataList[index].Prediction;
                                        if (typeof this.predictionOutcome !== 'string') {
                                            this.predictionOutcome = this.predictionOutcome.toFixed(2);
                                        }
                                    }
                                    if (_this.testDataList[index]['ProblemType'] === 'Multi_Class') {
                                        const probablityData = _this.setProbabilityData(_this.testDataList[index].Probablities);
                                        _this.testDataList[index]['OutcomePrediction'] = probablityData;
                                    } if (_this.testDataList[index]['ProblemType'] === 'Text_Classification') {
                                        this.isTextPreProcessing = true;
                                        if (testCaseData.ProblemTypeFlag === false) {
                                            this.isNLPClassification = true;
                                            _this.testDataList[index].Probablities.Probab1 = (Math.round(_this.testDataList[index].Probablities.Probab1 * 100));
                                            _this.testDataList[index].Probablities.Probab2 = (Math.round(_this.testDataList[index].Probablities.Probab2 * 100));
                                        } else if (testCaseData.ProblemTypeFlag === true) {
                                            this.isNLPClassification = false;
                                            const probablityData = _this.setProbabilityData(_this.testDataList[index].Probablities);
                                            _this.testDataList[index].Probablities = probablityData;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _this.appUtilsService.loadingEnded();
                }, error => {
                    _this.notificationService.error('Something went wrong in getTeachAndTestScenario.');
                    _this.appUtilsService.loadingEnded();
                });
        } else {
            this.disablePAToggle = false;
            if (this.PrescriptiveToogle) {
                this.PrescriptiveToogle.nativeElement.checked = false;
            }
            this.ngOnInit();
        }

    }
    setProbabilityData(probablitiesData) {
        const probablitiesList = [];
        let d = 0;
        Object.entries(probablitiesData).forEach(
            ([key, value]) => {
                const values = [];
                values.push(value);
                const percentVal = values[0];
                probablitiesList.push({
                    'featurename': key, 'prcntvalue': Math.round(percentVal * 100), 'color': this.getRandomColor(d++)
                });
            });
        return probablitiesList;
    }
    generateBarChartData(featureWeights) {
        this.chartData = [];
        let d = 0;
        Object.entries(featureWeights).forEach(
            ([key, value]) => {
                this.chartData.push({
                    'featurename': key, 'prcntvalue': value, 'color': this.getRandomColor(d++)
                });
            });
        return this.chartData;
    }
    getRandomColor(d) {
        /* const color = Math.floor(0x1000000 * Math.random()).toString(16);
        return '#' + ('000000' + color).slice(-6); */
        /* return 'hsl(' + 360 * Math.random() + ',' +
                 (25 + 70 * Math.random()) + '%,' +
                 (85 + 10 * Math.random()) + '%)'; */
        if (d % 2 === 1) {
            return '#10ADD3';
        } else {
            return '#286e99';
        }
    }
    runTest() {
        this._usageTrackingService.usageTracking('Create Custom Model', 'Model Predictions');
        const _this = this;
        let bool = true;
        this.wfId = null;
        if (this.coreUtilsService.isNil(this.selectedModelName)) {
            bool = false;
            _this.notificationService.error('Model Name is empty.Kindly Select Model from Recommand AI');
        }
        if (bool) {
            if (this.modelType === 'TimeSeries') {
                this.postRunTestForTimeSeries(_this, 'False');
            } else {
                this.postRunTestOrRunBulkTest(_this, 'False');
            }
        }
    }

    runBulkTest() {
        const _this = this;
        let bool = true;
        this.wfId = null;
        this.showRunTestbutton = true;
        if (this.coreUtilsService.isNil(this.selectedModelName)) {
            bool = false;
            _this.notificationService.error('Model Name is empty.Kindly Select Model from recommand AI');
        }
        if (bool) {
            this.postRunTestOrRunBulkTest(_this, 'True');
        }
    }

    postRunTestOrRunBulkTest(_this: any, isRunBulk: string) {
        const fetureData = {};
        let indexFeature = 1;
        this.isRunTestorRunBulkTest = isRunBulk;
        let countOfEmptyFields = 0;
        let onlySpaceCounter = 0;
        let onlySpecialChar = 0;
        const regexforOnlySpace = /^[\s*/] + $/;
        const regexSplChars = /[@_!#$%^&*()<>,?/|}{~:]+$/;
        Object.entries(this.postFeatureList[0]).forEach(
            ([key, value]) => {
                if (value['Type'] === 'text') {
                    const valueOfText = value['Value'];
                    if (valueOfText === '') {
                        countOfEmptyFields++;
                    }
                    const isOnlySpaces = regexforOnlySpace.test(valueOfText);
                    const isOnlySpecialChar = regexSplChars.test(valueOfText);
                    if (isOnlySpecialChar) {
                        onlySpecialChar++;
                    } else if (isOnlySpaces) {
                        onlySpaceCounter++;
                    }
                    value['Value'] = valueOfText.trim();
                }
                if (value['Type'] === 'float') {
                    value['Value'] = parseFloat(value['Value']);
                }
                fetureData['Feature' + indexFeature] = value;
                indexFeature++;
            });
        if (countOfEmptyFields === 0 && onlySpecialChar === 0 && onlySpaceCounter === 0) {
            this.appUtilsService.loadingStarted();
            this.runtestSubscription = this.teachTestService.postWhatIfAnalysisFeatures(this.correlationId,
                this.appUtilsService.getCookies().UserId,
                fetureData, this.selectedModelName, isRunBulk, this.modelType, _this.wfId).pipe(
                    switchMap(
                        data => {
                            let tempData = {};
                            if (this.IsJsonString(data)) {
                                const parsedData = JSON.parse(data);
                                tempData = parsedData;
                            } else if (data.constructor === Object) {
                                tempData = data;
                            } else if (data.constructor === String) {
                                // this.noData = data;
                                this.ns.success(data);
                            }
                            if (tempData.hasOwnProperty('Message') && tempData['Message'] === 'Success') {
                                return this.teachTestService.postWhatIfAnalysisFeatures(this.correlationId,
                                    this.appUtilsService.getCookies().UserId,
                                    fetureData, this.selectedModelName, isRunBulk, this.modelType, tempData['WFId']);
                            } else if (tempData.hasOwnProperty('PredictionData')) {
                                /* Bug 700895 Ingrain_StageTest_R2.1 - Sprint 2 - Teach and Test
                                - When we click on Run Bulk Test its throwing an error message "Run Test api has failed"
                                Fix - Added check for NULL */
                                if (!this.coreUtilsService.isNil(tempData['PredictionData'])) {
                                    const predictions = tempData['PredictionData'][0].Predictions;
                                    if (Object.keys(predictions).length === 0) {
                                        this.ns.error('Auto run test charts are not available as the text input is mandatory to be entered.');
                                    }
                                }
                                return of(tempData);
                            } else {
                                // this.noData = 'Format from server is not Recognised';
                                this.ns.error(`Error occurred:
                  Due to some backend data process the relevant data could not be produced.
                  Please try again while we troubleshoot the error`);
                                return EMPTY;
                            }

                        }
                    ),
                    catchError(data => {
                        if (data.hasOwnProperty('error')) {
                            if (data.error['Status'] === 'E') {
                                this.appUtilsService.loadingImmediateEnded();
                                // this.isLoading = false;
                                this.ns.error(data.error['Message']);
                            }
                        } else {
                            this.ns.error('Run test api has failed');
                        }
                        this.appUtilsService.loadingImmediateEnded();
                        return EMPTY;
                    })
                ).subscribe(
                    data => {
                        if (data['Status'] === 'P') {
                            _this.wfId = data.WFId;
                            this.retry(_this);
                        } else if (data['Status'] === 'C') {
                            this.openFeatureValues = true;
                            this.disablePrescriptiveButton = false;
                            this.disablePAToggle = false;
                            this.disableReset = false;
                            this.appUtilsService.loadingImmediateEnded();
                            this.isLoading = false;
                            this.collapseFeatureValues(true);
                            if (!_this.coreUtilsService.isNil(data.PredictionData)) {
                                this.runTestData = JSON.parse(JSON.stringify(data.PredictionData));
                                _this.setFeaturePredictionData(_this, data.PredictionData);
                            }
                            _this.wfId = data.WFId;
                            if (isRunBulk === 'True') {
                                _this.notificationService.success('Run Bulk Test completed');
                            } else {
                                _this.notificationService.success('Run Test completed');
                            }
                            _this.appUtilsService.loadingImmediateEnded();
                        } else if (data['Status'] === 'E') {
                            _this.appUtilsService.loadingImmediateEnded();
                            this.ns.error(data['Message']);
                        } else {
                            this.ns.error(`Error occurred: Due to some backend data process
            the relevant data could not be produced. Please try again while we troubleshoot the error`);
                            this.unsubscribe();
                            _this.appUtilsService.loadingImmediateEnded();
                        }
                    });
        } else if (countOfEmptyFields > 0) {
            _this.appUtilsService.loadingImmediateEnded();
            this.notificationService.error('Kindly enter values for all mandatory text boxes.');
        } else if (onlySpaceCounter > 0) {
            _this.appUtilsService.loadingImmediateEnded();
            this.notificationService.error('No empty spaces allowed in text field.');
        } else if (onlySpecialChar > 0) {
            _this.appUtilsService.loadingImmediateEnded();
            this.notificationService.error('No special characters with spaces allowed in text field.');
        }
    }

    retry(_this) {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.postRunTestOrRunBulkTest(_this, this.isRunTestorRunBulkTest));
        return this.timerSubscription;
    }

    unsubscribe() {
        if (!this.coreUtilsService.isNil(this.timerSubscription)) {
            this.timerSubscription.unsubscribe();
        }
        if (!this.coreUtilsService.isNil(this.runtestSubscription)) {
            this.runtestSubscription.unsubscribe();
        }
        this.appUtilsService.loadingEnded();
    }

    postRunTestForTimeSeries(_this: any, isRunBulk: string) {
        this.appUtilsService.loadingStarted();
        this.runtestSubscriptionTimeSeries = this.teachTestService.postWhatIfAnalysisFeatures(this.correlationId,
            this.appUtilsService.getCookies().UserId,
            this.stepsValue, this.selectedModelName, isRunBulk, this.modelType, this.wfId).pipe(
                switchMap(
                    data => {
                        let tempData = {};
                        if (this.IsJsonString(data)) {
                            const parsedData = JSON.parse(data);
                            tempData = parsedData;
                        } else if (data.constructor === Object) {
                            tempData = data;
                        } else if (data.constructor === String) {
                            // this.noData = data;
                            this.ns.success(data);
                        }
                        if (tempData.hasOwnProperty('Message') && tempData['Message'] === 'Success') {
                            return this.teachTestService.postWhatIfAnalysisFeatures(this.correlationId,
                                this.appUtilsService.getCookies().UserId,
                                this.stepsValue, this.selectedModelName, isRunBulk, this.modelType, tempData['WFId']);
                        } else if (tempData.hasOwnProperty('PredictionData')) {
                            return of(tempData);
                        } else {
                            // this.noData = 'Format from server is not Recognised';
                            this.ns.error(`Error occurred:
                  Due to some backend data process the relevant data could not be produced.
                  Please try again while we troubleshoot the error`);
                            return EMPTY;
                        }
                    }
                ),
                catchError(data => {
                    if (data.hasOwnProperty('error')) {
                        if (data.error['Status'] === 'E') {
                            this.appUtilsService.loadingImmediateEnded();
                            // this.isLoading = false;
                            this.ns.error(data.error['Message']);
                        }
                    } else {
                        this.ns.error('Run test api has failed');
                    }
                    this.appUtilsService.loadingImmediateEnded();
                    return EMPTY;
                })
            ).subscribe(
                data => {
                    if (data['Status'] === 'P') {
                        _this.wfId = data.WFId;
                        this.retryTimeseries(_this);
                    } else if (data['Status'] === 'C') {
                        _this.disableSavetestTimeSeries = false;
                        this.appUtilsService.loadingImmediateEnded();
                        if (!_this.coreUtilsService.isNil(data.PredictionData)) {
                            this.runTestData = data.PredictionData;
                            this.setLineChartData(data);
                        }

                        _this.wfId = data.WFId;
                        _this.notificationService.success('Run Test completed');
                        _this.appUtilsService.loadingImmediateEnded();
                    } else if (data['Status'] === 'E') {
                        _this.appUtilsService.loadingImmediateEnded();
                        this.ns.error(data['Message']);
                    } else {
                        this.ns.error(`Error occurred: Due to some backend data process
          the relevant data could not be produced. Please try again while we troubleshoot the error`);
                        this.unsubscribeTimeseries();
                        _this.appUtilsService.loadingImmediateEnded();
                    }
                }
            );
    }

    retryTimeseries(_this) {
        this.timerSubscriptionTimeSeries = timer(10000).subscribe(() => this.postRunTestForTimeSeries(_this, 'False'));
        return this.timerSubscriptionTimeSeries;
    }

    unsubscribeTimeseries() {
        if (!this.coreUtilsService.isNil(this.timerSubscriptionTimeSeries)) {
            this.timerSubscriptionTimeSeries.unsubscribe();
        }
        if (!this.coreUtilsService.isNil(this.runtestSubscriptionTimeSeries)) {
            this.runtestSubscriptionTimeSeries.unsubscribe();
        }
        this.appUtilsService.loadingEnded();
    }

    setLineChartData(data) {

        this.selectedModelLineChart = [];
        const line_ChartData = data['PredictionData'][0];
        this.freequecnyType = (!this.coreUtilsService.isNil(data['PredictionData'][0].Frequency) && data['PredictionData'][0].Frequency === 'H')
            ? 'Hourly' : data['PredictionData'][0].Frequency;
        for (let index = 0; index < line_ChartData.Forecast.length; index++) {
            const objLineChart = {};
            objLineChart['Forecast'] = line_ChartData.Forecast[index];
            objLineChart['RangeTime'] = line_ChartData.RangeTime[index];
            this.selectedModelLineChart[index] = objLineChart;
            this.isData = true;
        }
        this.lineChartDataCount = this.selectedModelLineChart.length;
        return this.selectedModelLineChart;
    }
    setFeaturePredictionData(_this: any, data: any) {
        _this.predictionData = data;
        _this.predictionDataList = [];
        Object.entries(data[0].Predictions).forEach(
            ([key, value]) => {
                _this.predictionDataList.push(value);
            });
        _this.predictionDataCount = _this.predictionDataList.length;

        _this.disableSavetest = false;
        _this.disableSavebtn = false;
        for (let index = 0; index < _this.predictionDataList.length; index++) {
            // Clustering Flag if it is false
            if (!this.coreUtilsService.isNil(_this.predictionDataList[index].FeatureWeights)) {
                const barchartdata = _this.generateBarChartData(_this.predictionDataList[index].FeatureWeights);
                _this.predictionDataList[index].FeatureWeights = barchartdata;
            }

            _this.predictionDataList[index]['ProblemType'] = this.coreUtilsService.isNil(data[0].ProblemType) ? '' : data[0].ProblemType;
            _this.predictionDataList[index]['targetColumn'] = data[0].Target;
            if (_this.predictionDataList[index]['ProblemType'] === 'classification') {
                _this.predictionDataList[index].Probablities.Probab1 = (Math.round(_this.predictionDataList[index].Probablities.Probab1 * 100));
                _this.predictionDataList[index].Probablities.Probab2 = (Math.round(_this.predictionDataList[index].Probablities.Probab2 * 100));
                const data1 = {};
                // Setting Prediction Outcome Chart data
                data1[_this.predictionDataList[index].Prediction] = _this.predictionDataList[index].Probablities.Probab1;
                data1[_this.predictionDataList[index].OthPred] = _this.predictionDataList[index].Probablities.Probab2;

                const barchartdata1 = _this.generateBarChartData(data1);

                _this.predictionDataList[index]['OutcomePrediction'] = barchartdata1;
            }
            if (_this.predictionDataList[index]['ProblemType'] === 'regression') {
                if (this.showRunTestbutton) {
                    if (this.PAInput === true) {
                        if (this.showRunBulkTestbutton === true) {
                            _this.predictionDataList[index].Prediction = _this.predictionDataList[index].Prediction.toFixed(2);
                        } else {
                            _this.predictionDataList[index].Prediction = _this.predictionDataList[index].Prediction;
                        }
                    } else {
                        _this.predictionDataList[index].Prediction = _this.predictionDataList[index].Prediction.toFixed(2);
                    }
                } else {
                    // if (!this.coreUtilsService.isNil(this.predictionOutcome)) {
                    //   _this.predictionDataList[index].Prediction = this.predictionOutcome;
                    // } else {
                    _this.predictionDataList[index].Prediction = _this.predictionDataList[index].Prediction.toFixed(2);
                    // }
                }
                this.predictionOutcome = _this.predictionDataList[index].Prediction;
                if (typeof this.predictionOutcome !== 'string') {
                    this.predictionOutcome = this.predictionOutcome.toFixed(2);
                }
            }
            if (_this.predictionDataList[index]['ProblemType'] === 'Multi_Class') {
                const probablityData = _this.setProbabilityData(_this.predictionDataList[index].Probablities);
                _this.predictionDataList[index]['OutcomePrediction'] = probablityData; // Setting Prediction Outcome Chart data
            } if (_this.predictionDataList[index]['ProblemType'] === 'Text_Classification') {
                this.isTextPreProcessing = true;
                if (data[0].ProblemTypeFlag === false) {
                    this.isNLPClassification = true;
                    _this.predictionDataList[index].Probablities.Probab1 = (Math.round(_this.predictionDataList[index].Probablities.Probab1 * 100));
                    _this.predictionDataList[index].Probablities.Probab2 = (Math.round(_this.predictionDataList[index].Probablities.Probab2 * 100));
                } else if (data[0].ProblemTypeFlag === true) {
                    this.isNLPClassification = false;
                    const probablityData = _this.setProbabilityData(_this.predictionDataList[index].Probablities);
                    _this.predictionDataList[index].Probablities = probablityData;
                }
            }
        }
    }

    getRangeOutput(value) {
        return parseFloat(value).toFixed(this.decimalPoint);
    }

    getTypeValue(key: any, values: any, type: any, minValue: any, maxValue: any) {
        if (type === 'float') {
            this.minNumberArr[key] = minValue;
            this.maxNumberArr[key] = maxValue;
            this.minNumberArr[key] = parseFloat(minValue).toFixed(this.decimalPoint);
            this.maxNumberArr[key] = parseFloat(maxValue).toFixed(this.decimalPoint);
        }
        if (type === 'string') {
            this.stringOptions[key] = Object.keys(values);
        }
        this.featureType[key] = type;
    }

    getStyles(value, minValue, maxValue) {
        const diff = maxValue - minValue;
        const number = value * 1;
        const rangefromMinValue = number - minValue;

        let left = 13;
        left = ((rangefromMinValue / diff) * 100);
        if (left > 80) {
            left = left - 13 - value.length;
        }
        if (left < 13) {
            left = left + 13;
        }

        if (number === minValue) {
            left = 13;
        }
        if (number === maxValue) {
            left = (87 - value.length);
        }

        const styles = {
            'position': 'absolute',
            'left': left + '%',
            'z-index': '1',
            'top': '-12px',
            'color': '#10ADD3',
            'font-size': '12px',
            'font-weight': '700'
        };
        return styles;
    }


    hyperTuningSelected() {
        this.hyperTunning = true;
    }
    whatIfSelected() {
        if (this.hyperTunning) {
            this.hyperTunning = false;
        }
    }

    saveHyperTuning() {

    }
    onSelectFreequencyChange(value: any) {
        if (this.coreUtilsService.isNil(value)) {
            this.showRunTestbuttonTimeSeries = true;
        } else {
            this.showRunTestbuttonTimeSeries = false;
        }
        this.stepsValue = value;
    }
    // @HostListener('window:resize', ['$event'])
    // onResize(event?) {
    //   this.screenWidth = window.innerWidth;
    // }

    downloadPredictive() {
        // this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
        const component = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    component.predictiveOutcome = [];
                    let predictiveoutcomeTable = [];
                    component.predictionDataList.forEach(element => {
                        const predictiveresult = { 'target': element.Prediction };
                        var data = element.Data;
                        data['target'] = element.Prediction;
                        component.predictiveOutcome.push(data);
                    });

                    const target = component.targetColumn;
                    for (var i = 0; i < component.predictiveOutcome.length; i++) {
                        var obj = component.predictiveOutcome[i];
                        obj[target] = obj["target"];
                        delete (obj["target"]);
                        predictiveoutcomeTable.push(obj);
                    }
                    component._excelService.exportAsExcelFile(predictiveoutcomeTable, 'DownloadedData');
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                //if (confirmationflag === true) {
                component.predictiveOutcome = [];
                let predictiveoutcomeTable = [];
                component.predictionDataList.forEach(element => {
                    const predictiveresult = { 'target': element.Prediction };
                    var data = element.Data;
                    data['target'] = element.Prediction;
                    component.predictiveOutcome.push(data);
                });

                const target = component.targetColumn;
                for (var i = 0; i < component.predictiveOutcome.length; i++) {
                    var obj = component.predictiveOutcome[i];
                    obj[target] = obj["target"];
                    delete (obj["target"]);
                    predictiveoutcomeTable.push(obj);
                }
                component.notificationService.success('Your Data will be downloaded shortly');
                component._excelService.exportAsPasswordProtectedExcelFile(predictiveoutcomeTable, 'DownloadedData').subscribe(response => {
                    component.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                    let binaryData = [];
                    binaryData.push(response);
                    let downloadLink = document.createElement('a');
                    downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                    downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
                    document.body.appendChild(downloadLink);
                    downloadLink.click();
                }, (error) => {
                    component.ns.error(error);
                });
                //  }
            });
        }
    }

    downloadPredictiveOnSave() {
        const component = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    component.predictiveOutcome = [];
                    let predictiveoutcomeTable = [];
                    component.testDataList.forEach(element => {
                        const predictiveresult = { 'target': element.Prediction };
                        var data = element.Data;
                        data['target'] = element.Prediction;
                        component.predictiveOutcome.push(data);
                    });

                    const target = component.targetColumn;
                    for (var i = 0; i < component.predictiveOutcome.length; i++) {
                        var obj = component.predictiveOutcome[i];
                        obj[target] = obj["target"];
                        delete (obj["target"]);
                        predictiveoutcomeTable.push(obj);
                    }
                    component._excelService.exportAsExcelFile(predictiveoutcomeTable, 'DownloadedData');
                }
            })
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                //if (confirmationflag === true) {
                component.predictiveOutcome = [];
                let predictiveoutcomeTable = [];
                component.testDataList.forEach(element => {
                    const predictiveresult = { 'target': element.Prediction };
                    var data = element.Data;
                    data['target'] = element.Prediction;
                    component.predictiveOutcome.push(data);
                });

                const target = component.targetColumn;
                for (var i = 0; i < component.predictiveOutcome.length; i++) {
                    var obj = component.predictiveOutcome[i];
                    obj[target] = obj["target"];
                    delete (obj["target"]);
                    predictiveoutcomeTable.push(obj);
                }
                component.notificationService.success('Your Data will be downloaded shortly');
                component._excelService.exportAsPasswordProtectedExcelFile(predictiveoutcomeTable, 'DownloadedData').subscribe(response => {
                    component.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                    let binaryData = [];
                    binaryData.push(response);
                    let downloadLink = document.createElement('a');
                    downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                    downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
                    document.body.appendChild(downloadLink);
                    downloadLink.click();
                }, (error) => {
                    component.ns.error(error);
                });
                //  }
            });
        }
    }


    downloadData() {
        let dataForDownload = {};
        const component = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    component.teachTestService.getTeachTestData(component.correlationId, component.modelType, component.frequency, component.selectedModelName)
                        .subscribe(colData => {
                            if (colData) {
                                component.notificationService.success('Your Data will be downloaded shortly');

                                Object.keys(colData['FeatureNameList'][0]).forEach(element => {
                                    if (element.trim() != component.targetColumn.trim()) {
                                        dataForDownload[element] = colData['FeatureNameList'][0][element];
                                    }
                                });

                                component.tableDataforDownload[0] = dataForDownload;
                                component._excelService.exportAsExcelFile(component.tableDataforDownload, 'TemplateDownloaded');
                            }
                        }, error => {
                            throwError(error);
                        });
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                //  if (confirmationflag === true) { 
                component.teachTestService.getTeachTestData(component.correlationId, component.modelType, component.frequency, component.selectedModelName)
                    .subscribe(colData => {
                        if (colData) {
                            component.notificationService.success('Your Data will be downloaded shortly');

                            Object.keys(colData['FeatureNameList'][0]).forEach(element => {
                                if (element.trim() != component.targetColumn.trim()) {
                                    dataForDownload[element] = colData['FeatureNameList'][0][element];
                                }
                            });

                            component.tableDataforDownload[0] = dataForDownload;
                            component._excelService.exportAsPasswordProtectedExcelFile(component.tableDataforDownload, 'TemplateDownloaded').subscribe(response => {
                                component.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                                let binaryData = [];
                                binaryData.push(response);
                                let downloadLink = document.createElement('a');
                                downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                                downloadLink.setAttribute('download', 'TemplateDownloaded' + '.zip');
                                document.body.appendChild(downloadLink);
                                downloadLink.click();
                                component.disableForTimeSeries = false;
                            }, (error) => {
                                component.ns.error(error);
                            });
                        }
                    }, error => {
                        throwError(error);
                    });
                //  }
            });
        }
    }

    prescriptiveAnalytics(predictionOutcome) {
        if (!this.coreUtilsService.isNil(predictionOutcome)) {
            this.appUtilsService.loadingStarted();
            if (typeof predictionOutcome !== 'string') {
                predictionOutcome = predictionOutcome.toString();
            }
            const requestPayload = {
                'CorrelationId': this.correlationId,
                'WFId': this.wfId,
                'Desired_Value': predictionOutcome,
                'ModelType': this.modelType,
                'CreatedByUser': this.appUtilsService.getCookies().UserId,
                'PageInfo': 'PrescriptiveAnalytics',
                'model': this.selectedModelName,
                'isNewRequest': true
            };
            this.savePrescriptiveAnalyticsDetails(requestPayload, true);
        } else {
            this.notificationService.error('Kindly enter a value');
        }
    }

    savePrescriptiveAnalyticsDetails(requestPayload, isNewReq) {
        this.appUtilsService.loadingStarted();
        if (isNewReq === false) {
            requestPayload['isNewRequest'] = false;
        } else {
            requestPayload['isNewRequest'] = true;
        }
        this.teachTestService.savePrescriptiveAnalytics(requestPayload).pipe(
            switchMap(
                data => {
                    let tempData = {};
                    if (this.IsJsonString(data)) {
                        const parsedData = JSON.parse(data);
                        tempData = parsedData;
                    } else if (data.constructor === Object) {
                        tempData = data;
                    } else if (data.constructor === String) {
                        this.ns.success(data);
                    }
                    if (tempData.hasOwnProperty('Message') && tempData['Message'] === 'Success') {
                        requestPayload['isNewRequest'] = false;
                        return this.teachTestService.savePrescriptiveAnalytics(requestPayload);
                    } else if (tempData.hasOwnProperty('TeachtestData')) {
                        return of(tempData);
                    } else {
                        this.ns.error(`Error occurred:
              Due to some backend data process the relevant data could not be produced.
              Please try again while we troubleshoot the error`);
                        this.unsubscribe();
                        return EMPTY;
                    }
                }
            ),
            catchError(error => {
                if (error.hasOwnProperty('error')) {
                    if (error['error'].Status === 'E') {
                        this.appUtilsService.loadingImmediateEnded();
                        this.ns.error(error['error'].Message);
                    }
                } else {
                    this.ns.error('PrescriptiveAnalytics api has failed');
                }
                this.appUtilsService.loadingImmediateEnded();
                return EMPTY;
            })
        ).subscribe(
            data => {
                if (data['Status'] === 'P' || data['Status'] === 'null') {
                    this.retryPrescriptiveAnalytisAPICall(requestPayload);
                } else if (data['Status'] === 'C') {

                    // this.notificationService.success('Data Saved');
                    // this.disablePrescriptiveButton = true;
                    // this.disableReset = false;
                    // this.disableSavetest = false;
                    this.postPrescriptiveAnalysis(data);
                    this.appUtilsService.loadingImmediateEnded();
                    this.unsubscribe();
                } else if (data['Status'] === 'E') {
                    this.appUtilsService.loadingImmediateEnded();
                    this.ns.error(data['Message']);
                    this.unsubscribe();
                } else {
                    this.ns.error(`Error occurred: Due to some backend data process
        the relevant data could not be produced. Please try again while we troubleshoot the error`);
                    this.unsubscribe();
                    this.appUtilsService.loadingImmediateEnded();
                }
            });
    }

    retryPrescriptiveAnalytisAPICall(payLoad) {
        this.timerSubscription = timer(10000).subscribe(() => this.savePrescriptiveAnalyticsDetails(payLoad, false));
        return this.timerSubscription;
    }

    resetWhatIfAnalysis(confirmModal: TemplateRef<any>) {
        //  this.modalRef = this._modalService.show(confirmModal, this.config);
        this.confirm();
    }

    // confirmation popup cancel event
    decline(): void {
        this.modalRef.hide();
    }

    // confirmation popup reset event
    confirm(): void {
        /*   this.modalRef.hide();
           this.appUtilsService.loadingStarted();
           const requestPayload = {
             'correlationId': this.correlationId,
             'wFId': this.wfId,
           };
           this.teachTestService.deletePrescriptiveAnalytics(requestPayload).subscribe(response => {
             this.appUtilsService.loadingEnded(); 
             this.notificationService.success('Values are reset');*/
        this.disablePrescriptiveButton = false;
        this.disablePAToggle = false;
        this.disableReset = true;
        this.disableSavetest = false;
        this.prescriptiveAnalysis = false;
        this.showRunTestbutton = false;
        this.predictionDataList = [];
        this.onTestScenarionChange('', '');
        /*     },
              error => {
                this.appUtilsService.loadingEnded();
                this.notificationService.error('Something went wrong');
              }); */
    }
    onChangeOfSelect(value) {
        this.predictionOutcome = value;
        this.prescriptiveAnalytics(this.predictionOutcome);
    }
    pAInputChange(value) {
        if (!this.coreUtilsService.isNil(value)) {
            // this.predictionOutcome = value;
            // this.predictionOutcome = Math.abs(this.predictionOutcome);
        }
    }
    postPrescriptiveAnalysis(testCaseData) {
        this.prescriptivevalues = [];
        const _this = this;
        this.presAnalysisData = [];
        if (!_this.coreUtilsService.isNil(testCaseData)) {
            if (!_this.coreUtilsService.isNil(testCaseData.TeachtestData.Predictions)) {
                this.prescriptive_desired_value = testCaseData.TeachtestData.Predictions.Prediction.Prediction;
                this.runTestData[0] = JSON.parse(JSON.stringify(testCaseData.TeachtestData));
                if (typeof this.prescriptive_desired_value !== 'string') {
                    // this.prescriptive_desired_value = Math.round(this.prescriptive_desired_value);
                    this.prescriptive_desired_value = this.prescriptive_desired_value.toFixed(2);
                    this.predictionOutcome = this.prescriptive_desired_value;
                }
                this.prescriptivevalues = this.predictionOutcomeValues.filter(x => x !== this.prescriptive_desired_value);
                this.notificationService.success('Run Test completed');
                this.disablePrescriptiveButton = true;
                this.disableReset = false;
                this.disableSavetest = false;

                // Bug 719335 Ingrain_StageTest_R2.1 - Sprint 2 - Data is mismatching for Prescriptive Analytics before and after saving the data
                // Fix :- Updated the Prediction chart, Feature chart based on data provided by Prescriptive Analytics
                const results = testCaseData.TeachtestData.Predictions.Prediction;
                const ProblemType = testCaseData.TeachtestData.ProblemType;

                const barchartdata = _this.generateBarChartData(results.FeatureWeights);
                this.predictionDataList[0].FeatureWeights = barchartdata;

                if (ProblemType === 'classification') {
                    results.Probablities.Probab1 = (Math.round(results.Probablities.Probab1 * 100));
                    results.Probablities.Probab2 = (Math.round(results.Probablities.Probab2 * 100));
                    const data1 = {};
                    // Setting Prediction Outcome Chart data
                    data1[results.Prediction] = results.Probablities.Probab1;
                    data1[results.OthPred] = results.Probablities.Probab2;
                    const barchartdata1 = _this.generateBarChartData(data1);
                    this.predictionDataList[0]['OutcomePrediction'] = barchartdata1;
                }
                if (ProblemType === 'Multi_Class') {
                    const probablityData = _this.setProbabilityData(results.Probablities);
                    _this.predictionDataList[0]['OutcomePrediction'] = probablityData; // Setting Prediction Outcome Chart data
                }

                this.presAnalysisData.push(testCaseData.TeachtestData.Predictions.Prediction);
                this.prescriptiveDonutValue = this.predictionOutcome;
                this.prescriptiveAnalysis = true;
                this.showRunTestbutton = true;
            } else {
                this.notificationService.error('Inadequate data: Data quality is too low for ingrAIn to suggest the prescriptive measures. Please refine the data to make it adequate and more exhaustive.');
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
    randomInitialValues(minVal, maxVal) {
        return parseFloat(Math.floor(Math.random() * (maxVal - minVal)) + minVal).toFixed(this.decimalPoint);
    }
    sliderInputChange(minValue, maxValue, inputValue, index, key) {
        if (inputValue < minValue) {
            this.postFeatureList[index][key].Value = minValue;
        }
        if (inputValue > maxValue) {
            this.postFeatureList[index][key].Value = maxValue;
        }
    }
    orderByRowNum = (a, b) => {
        return a;
    }

    collapseFeatureValues(isFromDropdownChange) {
        if (isFromDropdownChange === false) {
            this.openFeatureValues = this.openFeatureValues === true ? false : true;
        }
        if (this.openFeatureValues) {
            this.featureChartsHWClassification.width = 370;
            //  this.featureChartsHWRegression.width = 230;
            this.PredictionOutcomeChart.width = 500;
        } else {
            this.featureChartsHWClassification.width = 515;
            //  this.featureChartsHWRegression.width = 330;
            this.PredictionOutcomeChart.width = 900;
        }
        return this.openFeatureValues;
    }
    showFeatureWeight(predictions) {
        if (predictions.hasOwnProperty('WordCloud') && (predictions.WordCloud['image'] !== ''
            || predictions.WordCloud['message'] !== '')) {
            this.showWordCloud = true;
        } else {
            this.showWordCloud = false;
        }

        if (this.clusteringFlag == 'false') {
            return false;
        } else {
            return true;
        }
    }
    /* onPIConfirmation(optionValue) {
      this.PIConfirmation = optionValue;
    } */

    rangeCss(value, minValue, maxValue) {
        let rangeVal = Math.ceil(((value - minValue) / (maxValue - minValue)) * 100) || 0;
        const styles = {
            '--range-size': (rangeVal + '%')
        };
        return styles;
    }
    teachAndTestToggle(event) {
        let requiredUrl = 'dashboard/modelengineering/TeachAndTest/WhatIfAnalysis';
        if (event.currentTarget.checked === true) {
            requiredUrl = 'dashboard/modelengineering/TeachAndTest/HyperTuning';
        }
        this._router.navigateByUrl(requiredUrl);
    }
    togglePA(event) {
        if (event.currentTarget.checked === true) {
            this.pASelected = true;
        } else {
            // if (this.modelType === 'regression' || this.modelType === 'Regression') {
            //     if(this.disablePrescriptiveButton === false){
            //     const _this = this;    
            //      this.runTestDatacpoy =  JSON.parse(JSON.stringify(this.runTestData));
            //  this.setFeaturePredictionData( _this,this.runTestDatacpoy);
            //      }
            this.PAInput = true;
            this.pASelected = false;
        }

        //  }

    }

}
