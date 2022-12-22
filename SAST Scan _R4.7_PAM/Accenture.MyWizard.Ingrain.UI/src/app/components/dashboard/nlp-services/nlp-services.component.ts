import { Component, OnInit, ViewChild, ElementRef, Renderer2, TemplateRef } from '@angular/core';
import { ExcelService } from 'src/app/_services/excel.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ProblemStatementService } from '../../../_services/problem-statement.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DialogService } from 'src/app/dialog/dialog.service';
import { TemplateNameModalComponent } from '../../template-name-modal/template-name-modal.component';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { UploadSimilarityAnalysisAPIComponent } from 'src/app/components/upload-similarity-analysis-api/upload-similarity-analysis-api.component';
import { FileUploadProgressBarComponent } from 'src/app/components/file-upload-progress-bar/file-upload-progress-bar.component';
import { UploadFileComponent } from 'src/app/components/upload-file/upload-file.component';
import { empty, throwError, timer, of, pipe, EMPTY, Subject } from 'rxjs';
import { IngestDataColumnNamesComponent } from 'src/app/components/ingest-data-column-names/ingest-data-column-names.component'
import { ucs2 } from 'punycode';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ShowDataComponent } from 'src/app/components/dashboard/data-engineering/preprocess-data/show-data/show-data.component';
import { FormBuilder, FormGroup } from '@angular/forms';
import { isArray } from 'util';
import * as _ from 'lodash';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { NotificationData } from 'src/app/_services/usernotification';
import * as $ from 'jquery';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { ApiService } from 'src/app/_services/api.service';
import { Options } from 'ng5-slider';
import { saveAs } from 'file-saver';
import { CustomDataViewComponent } from './custom-data-view/custom-data-view.component';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { serviceLevel, ServiceTypes } from 'src/app/_enums/service-types.enum';
import { DeployModelService } from 'src/app/_services/deploy-model.service';
declare var userNotification: any;


interface IParamData {
    clientUID: any;
    parentUID: any;
    deliveryConstructUId: any;
}

@Component({
    selector: 'app-nlp-services',
    templateUrl: './nlp-services.component.html',
    styleUrls: ['./nlp-services.component.scss'],
    animations: [
        trigger('detailExpand', [
            state('collapsed', style({ height: '0px', minHeight: '0', display: 'none' })),
            state('expanded', style({ height: '*' })),
            transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
        ]),
    ]
})
export class NlpServicesComponent implements OnInit {
    @ViewChild('fileInput', { static: false }) fileInput: any;
    @ViewChild('searchInput', { static: false }) searchInput: any;

    paramData = {} as IParamData;
    selectedServicelongdescription: string;
    selectedServiceName: string;
    selectedServiceId: string;

    uploadedData = [];
    selectedModelCorrelationId: string;
    selectedModelStatus: string;
    enteredModelName: string;

    payloadForGetAICoreModels = {
        'clientid': '',
        'dcid': '',
        'serviceid': '',
        'userid': ''
    };

    disabledRetrain = true;
    disabledTrain = true;
    trainedModels: any;
    textForEvaluation = '';
    modelSelectedForEvaluate = false;
    evaluatedResponse: any;
    rawEvaluatedData: any;
    responseType = 'formatted';
    disabledEvaluate = true;
    templateFlag = '1';
    rangeValue;
    rangeValuemax;
    isClustering = false;
    isWordCloud = false;
    downloadTemplate = [
        { 'fileName': 'Template.xlsx', 'allowedFileType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.csv,.json' },
        { 'fileName': 'sentence_similarity.xlsx', 'allowedFileType': '.txt' },
        { 'fileName': 'next_word_prediction.xlsx', 'allowedFileType': '.txt' },
        { 'fileName': '', 'allowedFileType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.csv,' },
        { 'fileName': 'similarity_analysis.xlsx', 'allowedFileType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.csv,.json' },
        { 'fileName': 'insurance.csv', 'allowedFileType': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,.csv,' }
    ];
    fileName;
    allowedFileType;
    clusteringType;
    isKmeansChecked = false;
    isAgglomerativeChecked = false;
    isDbscanChecked = false;
    numberOfCluster = 1;
    autoClustering = true;
    switches = ['KMeans', 'DBSCAN', 'Agglomerative'];
    selectedOptions = {
        'KMeans': { 'Train_model': 'False', 'Auto_Cluster': 'True', 'No_of_Clusters': 1 },
        'DBSCAN': { 'Train_model': 'False' },
        'Agglomerative': { 'Train_model': 'False' }
    };
    problemType = {
        'Clustering': {
            'Text': 'True',
            'Non-Text': 'False'
        }
    };
    modelTypes;
    clusteredModels;
    columnsForEvaluate;
    visualisationData;
    textColumns = [];
    nonTextColumns = [];
    categoryColumns = [];
    token;
    modelTypeForEval;
    selectedModelsForEval = {
        'KMeans': {
            'Train_model': 'True',
        },
        'DBSCAN': {
            'Train_model': 'False'
        },
        'Agglomerative': {
            'Train_model': 'False'
        }
    };
    allowedExtensions = ['.xlsx', '.csv'];
    filteredTrainedModels = [];
    filterType = 'All';
    oldProblemType;
    oldSelectedOptions;
    selectedUniId: string;
    columnsToDisplay = ['selection', 'Model Name', 'Service URL', 'Data Source', 'Last Trained Date', 'Model Type', 'Page Info', 'Model Status', 'Action Item', 'function', 'deleteselection', 'recordsCount'];
    columnsToDisplayWC = ['selection', 'Model Name', 'Data Source', 'Last Trained Date', 'Page Info', 'Model Status', 'function', 'deleteselection'];
    expandedElement;
    clusterMapping = { 'ClusterInputs': { 'KMeans': {}, 'DBSCAN': {} } };
    selectionChangeClusterMapping = {};
    bulkData = {};

    // PIConfirmation = false;
    similarityAnalysis = false;
    Myfiles: any;
    modelName: any;
    correlationId: string;
    filteredMetricData;
    deliveryTypeName;
    entityData;
    ingestStatusSubscription: any;
    timerSubscription: any;
    selectedIngestedColumnNames = [];
    monitorIngestDataPayload: any;
    disabledTrainSim = true;
    disabledReTrainSim = true;
    trainedModelColumns: any;
    selectedModelData: any;
    oldSelectedColumns = [];
    selectedDataSource: string;
    modelIsSelected = false;
    evalPredictionHeaders = [];
    trainSimilarityModelMsg: string;

    // UseCase Tabs
    showNavigatonTabs = false;
    enablePublishUseCaseTab: boolean = false;
    enableModelTab = false;
    publishUseCases;
    isSampleInputPopupClosed = false;
    usecaseSampleInputshow = false;
    selectedusecaseSampleInputIndex: number;
    disableUseCaseTrain = true;
    isUsecasePopOverOpen = false;
    isDeveloperPrediction = false;
    selectedusecaseSampleInputBtnIndex: number;
    selectedModelForPopoverIndex: number;
    usecase_name = null;
    ifUsecaseModelSelected;
    // UseCase Tabs

    modalRefForViewData: BsModalRef | null;
    config = {
        ignoreBackdropClick: true,
        class: 'preprocess-addfeature-manualmodal'
    };
    viewDataForCLustering = [];
    disableDwnldMappedData = true;
    columnSelectValForEvaluate;
    selectedClusterValue;
    useCaseAttributes;
    useCaseStopWords;
    disabledTextNontext = false;
    language: string;
    langTranslationOptions = [
        { name: 'English to German', value: 'English-German' },
        { name: 'German to English', value: 'German-English' },
        { name: 'English to Russian', value: 'English-Russian' },
        { name: 'Russian to English', value: 'Russian-English' },
        { name: 'English to French', value: 'English-French' }
    ];
    selectedLangTranslation = '';
    isnavBarToggle = false;
    isNavBarLabelsToggle = false;

    // Word Cloud
    disabledTrainWC = true;
    disabledGenerateWC = true;
    stopWordsWC = '';
    stopWordListWC = [];
    selectedWCImage = '';
    toggleSelect = true;
    isDelete = false;
    isFlag = false;
    isPublishusecase = true;
    isGCWordCloud = true;

    fileView: boolean;
    entityView: boolean;

    datasetView: boolean;
    customDataView: boolean = false;
    isAdminUser: boolean = false;
    entityFormGroup: FormGroup;
    entityList = [];
    allEntities = [];
    entityArray = [];

    datasetArray = [];
    datasetEntered: boolean = false;
    selectedtype;

    deliveryTypeList: Array<string>;
    selectedDeliveryType: any;
    selectedEntity;
    entityLoader = true;
    reTrainModel: boolean = false;
    startDate = new Date();
    clusterChartTextView = false;
    clusterChartNonTextView = false;

    treeMapData = null;
    bubbleChartData = null;
    barGroupData = null;
    setOfWordCloud: any;
    Bulkmultiple: string;
    ngramOne: Number = 2;
    ngramTwo: Number = 3;
    options: Options = {
        floor: 1,
        ceil: 4,
        step: 1,
        noSwitching: true
    };
    stopWordscluster = '';
    clusteringSW = [];
    selectedbulk_multiple: boolean;
    disableDownload = true;
    // singlecheck = false;

    datasetname;
    datasetname2;
    choosetype = [];
    selectedsource;
    choosesource = [
        { name: "File", value: "File" },
        { name: "API", value: "API" },
    ];
    env: string;
    requestType;
    validationFlag: boolean;
    searchText;
    coreModlesList;
    corePublishModelList;
    oldSeletedStopWords = [];
    oldNgramSelection;
    selectedNgramVal = [];
    uniqueScore;
    jsondata;
    totalPageCount;
    noofpage;
    toggle: boolean = true;
    userId: string;
    suggestedKeyIndexVal;
    totalCluster;
    selectDatasetUI;
    selectedValue: any;
    selectedNewValue: any;
    wordCloudValue: any;
    modelStatusValue: any;
    ingestDataColumnValue: any;
    compareWordDataValue: any;
    modelColumnValue: any;
    value07: any;
    originalData = [];
    originalDataCheck: any;
    newDataCheck: any;
    compareTwoObjects: boolean;
    trainingDataVolume: boolean;
    provideTrainingDataVolume: string = '';
    selectedDataSetSourcetype = '';
    isValidDataVolume = false;
    textChanged: Subject<string> = new Subject<string>();
    backUpClusteringModels: any;
    entityList2 = [
        { name: 'Incidents', subDeliverable: 'ALL' },
        { name: 'ServiceRequests', subDeliverable: 'ALL' },
        { name: 'ProblemTickets', subDeliverable: 'ALL' },
        { name: 'WorkRequests', subDeliverable: 'ALL' },
    ];
    selectedEntity2;

    reTrainingFrequency: Array<any> = [
        { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
        { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Fortnightly', FrequencyVal: ['1'] },
    ];

    reTrainingFrequencyValue: Array<any>;

    selectedOfflineRetrain = false;

    selectedRFrequency: any;
    rFrequency: any;
    selectedRFeqValues: any;
    IsOnline = false;
    IsOffline = false;
    @ViewChild('offlineRetrainingSelected') offlineRetrainingSelected: ElementRef;
    entityStartDate = '';
    entityEndDate = '';
    selectedModel;
    currentSuggestion;
    pageloadCluster;
    isDevTest = false;
    apiData: any;
    QueryData: any;
    decimalPoint;
    RecordsCountMessage: string = '';
    enableRecordsCountView: boolean = false;
    ConfigStorageKey = 'CustomConfiguration';
    infoMessage = `<div>
  <b>Export File Password Hint:</b><br>
  &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
  &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
  </div>`;
    isShowInfoIcon = false;
    instanceType: string;

    constructor(private _appUtilsService: AppUtilsService, private _excelService: ExcelService,
        private problemStatementService: ProblemStatementService, private ns: NotificationService, private route: ActivatedRoute,
        private router: Router, private dialogService: DialogService, private coreUtilService: CoreUtilsService,
        private renderer: Renderer2, private elem: ElementRef, private _modalService: BsModalService, private formBuilder: FormBuilder,
        private envService: EnvironmentService, private api: ApiService, private _deploymodelService: DeployModelService) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
    }

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        const ingrainURL = this.api.ingrainAPIURL.toLowerCase();

        if (ingrainURL.includes('stagept') || ingrainURL.includes('devtest') || ingrainURL.includes('devut')) {
            this.isDevTest = true;
        }
        if (this.envService.IsPADEnvironment()) {//enable custom data option only for PAD environmant
            this.verifyAdminUser();
        }
        this.textChanged.pipe(debounceTime(1000), distinctUntilChanged())
            .subscribe(model => {
                this.isSpecialCharacterMaxData(model);
            });

        this.env = sessionStorage.getItem('Environment');
        this.requestType = sessionStorage.getItem('RequestType');
        this.userId = this._appUtilsService.getCookies().UserId;

        this.api.openDisclaimer();
        this.Bulkmultiple = 'Single';
        if (sessionStorage.getItem('Language')) {
            this.language = sessionStorage.getItem('Language').toLowerCase();
        } else {
            this.language = 'english';
        }
        window.scroll(0, 0);
        this._appUtilsService.getParamData().subscribe(data => {
            this.paramData = data;
            this.route.queryParams
                .subscribe(params => {
                    this.selectedServiceName = params.serviceName;
                    this.selectedServiceId = params.serviceId;
                    this.selectedServicelongdescription = params.longdescription;
                    this.templateFlag = params.templateFlag;

                    if (this.selectedServiceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') { // Clustering service
                        this.setFileView();
                        // this.graphicalView();
                        this.isClustering = true;
                        this.similarityAnalysis = false;
                        this.enableModelTab = true;
                        this.showNavigatonTabs = true;
                    }

                    if (this.selectedServiceId === '042468f4-db5b-403f-8fbc-e5378077449e') { // Word Cloud service
                        this.setFileView();
                        this.isWordCloud = true;
                        this.isClustering = false;
                        this.similarityAnalysis = false;
                        this.enableModelTab = true;
                        this.showNavigatonTabs = true;
                        this.enablePublishUseCaseTab = false;
                    }

                    if (this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c' && this.templateFlag === '1') {
                        this.similarityAnalysis = false;
                        this.enableModelTab = true;
                    } else if (this.selectedServiceId !== '50f2232a-0182-4cda-96fc-df8f3ccd216c' && this.selectedServiceId !== '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355' && this.templateFlag === '1') {
                        this.similarityAnalysis = true;
                        this.setFileView();
                        this.showNavigatonTabs = true;
                        this.enablePublishUseCaseTab = false;
                        this.enableModelTab = true;
                    }
                    if (this.selectedServiceId === '28179a56-38b2-4d69-927d-0a6ba75da377') {
                        this.isDeveloperPrediction = true;
                        this.similarityAnalysis = false;
                        this.showNavigatonTabs = true;
                        this.enablePublishUseCaseTab = true;
                        this.enableModelTab = false;
                    }

                    if (this.env !== 'FDS' && this.env !== 'PAM') {
                        if (this.selectedServiceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355' || this.selectedServiceId === '93df37dc-cc72-4105-9ad2-fd08509bc823') { // Clustering service
                            this.isShowInfoIcon = true;
                        }
                    }

                    if (this.templateFlag === '1') {
                        let index = 0;
                        if (this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c') { // Intent and Entity Detection Service
                            index = 0;
                            this.enablePublishUseCaseTab = false;
                        } else if (this.selectedServiceId === 'a433dcb8-84af-4842-9fb3-74ddbb2d4beb') { // Get Text Similarity Service
                            index = 1;
                        } else if (this.selectedServiceId === 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') { // Predict Next Word Service
                            index = 2;
                        } else if (this.selectedServiceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') { // Clustering service
                            index = 3;
                        } else if (this.selectedServiceId === '93df37dc-cc72-4105-9ad2-fd08509bc823') { // Similarity Analytics
                            index = 4;
                        } else if (this.selectedServiceId === '042468f4-db5b-403f-8fbc-e5378077449e') { // word cloud
                            index = 5;
                        }
                        // if (!this.similarityAnalysis) {
                        this.fileName = this.downloadTemplate[index].fileName;
                        this.allowedFileType = this.downloadTemplate[index].allowedFileType;
                        // }

                    }

                    // Setting payload for GetAICoreModels
                    this.payloadForGetAICoreModels.clientid = this.paramData.clientUID;
                    this.payloadForGetAICoreModels.dcid = this.paramData.deliveryConstructUId;
                    this.payloadForGetAICoreModels.serviceid = this.selectedServiceId;
                    this.payloadForGetAICoreModels.userid = this._appUtilsService.getCookies().UserId;
                    // Populate AI core models
                    if (this.paramData.clientUID && this.paramData.deliveryConstructUId && this.templateFlag === '1' && this.isClustering === false && this.isWordCloud === false) {
                        this.getAICoreModels();
                    } else if (this.paramData.clientUID && this.paramData.deliveryConstructUId && (this.isClustering === true || this.isWordCloud === true)) {
                        this.getCusteringModels();
                    }

                    if (this.payloadForGetAICoreModels.clientid !== undefined) {
                        // this.getDynamicEntity(this.payloadForGetAICoreModels.clientid);
                    }

                    /** fetch all published usecases */
                    //   if (this.isClustering !== true) {
                    this.fetchAllUseCases();
                    //   }
                });
        });

        this.selectedsource = 'File';
        this.getdatasetdetails(this.selectedsource);
    }

    singleEval() {
        this.Bulkmultiple = 'Single';
        this.evaluatedResponse = undefined;
        this.rawEvaluatedData = undefined;
        // this.singlecheck = true;
    }

    bulkEval() {
        this.Bulkmultiple = 'Bulk';
        // this.singlecheck = false;
        this.evaluatedResponse = undefined;
        this.rawEvaluatedData = undefined;
    }

    checkboxSelectedIsOffline(elementRef) {
        if (elementRef.checked === true) {
            this.selectedOfflineRetrain = true;
        } else {
            this.selectedOfflineRetrain = false;
            this.selectedRFrequency = '';
            this.rFrequency = '';
            this.selectedRFeqValues = '';
        }
    }

    selectedRetrainingFrequency(rValue) {
        this.selectedRFrequency = rValue;
        this.rFrequency = rValue.name;
        this.reTrainingFrequencyValue = this.reTrainingFrequency.find(feq => feq.name == rValue.name).FrequencyVal;
        this.selectedRFeqValues = '';
    }

    selectedRFeqValue(pValue) {
        this.selectedRFeqValues = pValue;
    }


    reset() {
        this.disabledRetrain = true;
        this.disabledTrain = true;
        this.trainedModels = undefined;
        this.textForEvaluation = '';
        this.modelSelectedForEvaluate = false;
        this.evaluatedResponse = undefined;
        this.rawEvaluatedData = undefined;
        this.responseType = 'formatted';
        this.uploadedData = [];
        this.removeEntity();
        this.removeDataset();
        this.disabledEvaluate = true;
        this.selectedIngestedColumnNames = [];
        this.selectedModelCorrelationId = undefined;
        this.selectedModelStatus = undefined;
        this.enteredModelName = undefined;

        if (this.isClustering === true) {
            this.resetClusteringEvalData();
        }
        this.clearCustomData();
    }

    // Get AICoreModels
    getAICoreModels() {
        if (this.reTrainModel) {
            this.setFileView();
            this.reTrainModel = false;
        }

        this._appUtilsService.loadingStarted();
        this.trainedModels = undefined;
        this.trainedModelColumns = undefined;
        this.reset();
        this.problemStatementService.getAICoreModels(this.payloadForGetAICoreModels).subscribe(models => {
            // this._appUtilsService.loadingEnded();
            this.trainedModels = models['ModelStatus'];
            this.trainedModelColumns = models['ModelColumns'];

            let search = 'Progress';
            let results = _.filter(this.trainedModels, function (item) {
                return item.ModelStatus.indexOf(search) > -1;
            });

            if (results.length > 0) {
                this.disabledReTrainSim = true;
            }

            // if (models.length === 0) {
            if (this.trainedModels === null) {
                this.trainedModels = undefined;
            }
            this.coreModlesList = this.trainedModels;
            this.rawEvaluatedData = undefined;
            this.evaluatedResponse = undefined;
            this.modelSelectedForEvaluate = false;
            this.textForEvaluation = '';
            this.disableUseCaseTrain = true;

            this._appUtilsService.loadingEnded();
        }, error => {
            this.trainedModels = undefined;
            this._appUtilsService.loadingEnded();
            this.ns.error('Something went wrong.');
        });
        return false;
    }

    getFileDetails(event) {
        this.uploadedData = [];
        if (this.isClustering === true) {
            this.disabledRetrain = true;
            const fileName = event.target.files[0].name;
            const dots = fileName.split('.');
            const fileType = '.' + dots[dots.length - 1];
            if (this.allowedExtensions.indexOf(fileType) !== -1) {
                if (event.target.files[0].size <= 20971520) {
                    for (let i = 0; i < event.target.files.length; i++) {
                        this.uploadedData.push(event.target.files[i]);
                    }
                    this.fileInput.nativeElement.value = '';
                    this.disabledTrain = false;
                    if (this.modelSelectedForEvaluate === true && this.isClustering !== true) {
                        this.disabledRetrain = false;
                        this.disabledTextNontext = true;
                    }
                } else {
                    this.fileInput.nativeElement.value = '';
                    this.ns.error('Kindly upload file of size less than or equal to 20MB.');
                }
            } else {
                this.fileInput.nativeElement.value = '';
                this.ns.error('Kindly upload .xlsx or .csv file.');
            }
        } else {
            const fileName = event.target.files[0].name;
            const dots = fileName.split('.');
            const fileType = '.' + dots[dots.length - 1];
            if (this.allowedExtensions.indexOf(fileType) !== -1) {
                for (let i = 0; i < event.target.files.length; i++) {
                    this.uploadedData.push(event.target.files[i]);
                }
                this.fileInput.nativeElement.value = '';
                event.target.value = '';

                this.disabledTrain = false;
                if (this.modelSelectedForEvaluate === true) {
                    this.disabledRetrain = false;
                    this.disabledTextNontext = true;
                }
            } else {
                this.fileInput.nativeElement.value = '';
                this.ns.error('Kindly upload .xlsx or .csv file.');
            }
        }
    }

    downloadData() {
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    window.location.href = "assets/files/" + self.fileName;
                    self.ns.success('Your Data will be downloaded shortly');
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                window.location.href = "assets/files/" + self.fileName;
                self.ns.success('Your Data will be downloaded shortly');
            });
        }

        return false;
    }

    openModalPopUp() {

        const fileUploadAfterClosed = this.dialogService.open(UploadFileComponent, {}).afterClosed
            .pipe(
                tap(filesData => filesData ? this.openModalNameDialog(filesData, false) : '')
            );

        fileUploadAfterClosed.subscribe();
    }

    openModalNameDialog(filesData, retrain) {
        console.log('Nlp Service---', filesData)
        this.Myfiles = filesData;
        this.showModelEnterDialog(filesData, retrain);
    }

    /* Enter Model Name Dialog */
    showModelEnterDialog(filesData, retrain) {
        if (retrain) {
            this.enteredModelName ? this.openFileProgressDialog(filesData, this.enteredModelName, retrain) : '';
        } else {
            const openTemplateAfterClosed =
                this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name', pageInfo: 'IngestData' } }).afterClosed.pipe(
                    tap(modelName => {
                        if (this.ifUsecaseModelSelected !== undefined && modelName !== undefined) {
                            this.trainAIModelsFromUseCase(this.ifUsecaseModelSelected, modelName, filesData);
                        } else if (modelName !== undefined) {
                            this.openFileProgressDialog(filesData, modelName, retrain);
                        }
                    })
                );
            openTemplateAfterClosed.subscribe();
        }
    }

    /* Show File Upload Process Dialog */
    openFileProgressDialog(filesData, modelName, retrain) {
        this.modelName = modelName;
        const totalSourceCount = filesData[0].sourceTotalLength;
        const openFileProgressAfterClosed = this.dialogService.open(FileUploadProgressBarComponent,
            {
                data: { filesData: filesData, modelName: modelName, retrain: retrain, selectedServiceId: this.selectedServiceId }
            }).afterClosed.pipe(
                tap(data => {
                    if (data) {
                        if (retrain) {
                            if (data.body.IsSuccess) {
                                // if (data.message) {
                                this.ns.success('Re-train Initiated.');
                                this.reTrainModel = false;
                                // }
                            }
                            this.disabledTrainSim = true;
                            this.disabledReTrainSim = true;
                            this.selectedIngestedColumnNames = [];
                            this.disabledEvaluate = true;
                        } else {
                            this.monitorIngestDataPayload = data.body.CorrelationId;
                            this.selectedUniId = data.body.UniId;
                            this.disabledTrainSim = false;
                            this.disabledReTrainSim = true;

                            if (this.isClustering === true) {
                                this.disabledTrain = false;
                                this.uploadedData.push(1);
                            }
                        }
                        if (this.isClustering !== true && this.isWordCloud !== true) {
                            this.getAICoreModels();
                        } else {
                            this.getCusteringModels();
                        }
                    }
                })
            );
        openFileProgressAfterClosed.subscribe();
    }

    enableDisable(input) {
        if (input) {
            //Enable the TextBox when TextBox has value.
            this.disabledGenerateWC = false;
        } else {
            //Disable the TextBox when TextBox is empty.
            this.disabledGenerateWC = true;
        }
    };

    // Add words
    addWords(detail) {

        let stopwordFlag = 0;
        if (this.stopWordsWC === '') {

        } else {
            // Start Input Validation - DIYA Scanning
            const valid = this.coreUtilService.isSpecialCharacter(this.stopWordsWC);
            if (valid === 0) {
                return 0;
            } else {
                this.trainedModels.forEach(model => {
                    if (model.CorrelationId == detail.CorrelationId) {
                        if (model.StopWords.length == 0 && model.ingestData.StopWords === null) {
                            model.StopWords.push(this.stopWordsWC);
                        }
                        else {
                            if (model.ingestData.StopWords !== null) {
                                if (model.ingestData.StopWords.length > 0) {
                                    for (let i = 0; i < model.ingestData.StopWords.length; i++) {
                                        if (model.ingestData.StopWords[i] === this.stopWordsWC) {
                                            this.ns.error('Stop word has been already added to the list');
                                            return;
                                        } else {
                                            stopwordFlag = 1;
                                        }
                                    }
                                }
                            }
                            for (let i = 0; i < model.StopWords.length; i++) {
                                if (model.StopWords[i] === this.stopWordsWC) {
                                    this.ns.error('Stop word has been already added to the list');
                                    return;
                                } else {
                                    stopwordFlag = 1;
                                }
                            }
                            if (stopwordFlag === 1) {
                                model.StopWords.push(this.stopWordsWC);
                            }
                        }
                    }
                });

                this.stopWordsWC = '';
            }
            // End Input Validation - DIYA Scanning
        }
    }

    // Remove words
    removeWords(detail, index) {
        this.trainedModels.forEach(model => {
            if (model.CorrelationId == detail.CorrelationId) {
                model.StopWords.splice(index, 1);
            }
        });
    }

    removepopulatedWords(detail, index) {
        this.trainedModels.forEach(model => {
            if (model.CorrelationId == detail.CorrelationId) {
                model.ingestData.StopWords.splice(index, 1);
            }
        });
    }

    trainSimModel(modelCorrelationId?, trainedModelStatus?) {

        if (!this.checkCorrelationId(modelCorrelationId)) {
            return false;
        } else if (trainedModelStatus === 'O') {
            this.ns.warning(`Model training has been initiated and it is in occupied state, Please refresh the model to see the updated model status.`);
            return false;
        } else {
            this.reTrainModel = false;
            if (this.isClustering === true) {
                if ((this.numberOfCluster < 2 || this.numberOfCluster > 15) && this.selectedOptions['KMeans']['Auto_Cluster'] === 'False' && this.selectedOptions['KMeans']['Train_model'] === 'True') {
                    this.ns.error('Number of clusters should not be greater than 15 and less than 2.');
                } else if (this.selectedOptions.KMeans.Train_model === 'False' && this.selectedOptions.DBSCAN.Train_model === 'False') {
                    this.ns.error('Kindly select atleast one Model Type');
                } else {
                    this.monitorIngestedData(this.selectedModelCorrelationId, false);
                }
            } else {
                this.selectedModelData = undefined;
                if (this.modelIsSelected) {
                    this.monitorIngestedData(this.selectedModelCorrelationId, false);
                } else {
                    this.monitorIngestedData(this.monitorIngestDataPayload, false);
                }
            }
        }
        return false;
    }

    monitorIngestedData(payload, retrain) {
        const _data = payload;
        this._appUtilsService.loadingStarted();
        this.ingestStatusSubscription = this.problemStatementService.getAIServiceIngestStatus(payload, 'IngestData', this.selectedServiceId)
            .subscribe(
                data => {
                    if (data['Status'] === 'P' || data['Status'] === null) {
                        this.retry(_data, retrain);
                    } else if (data['Status'] === 'C') {
                        this._appUtilsService.loadingImmediateEnded();
                        this.selectedUniId = data.UniId;
                        if (data['ColumnNames'].length === 0) {
                            this.ns.error('Text columns are not available for this dataset');
                            return 0;
                        } else {
                            this.ns.success('Successfully Ingested Data');
                            this.ingestDataColumnNames(data, retrain);
                        }
                    } else if (data['Status'] === 'E') {
                        this._appUtilsService.loadingImmediateEnded();
                        this.ns.error(data['Message']);
                    } else {
                        this.ns.error(`Error occurred: Due to some backend data process
          the relevant data could not be produced. Please try again while we troubleshoot the error`);
                        this.unsubscribe();
                        this._appUtilsService.loadingImmediateEnded();
                    }
                }, error => {
                    this._appUtilsService.loadingImmediateEnded();
                    this.ns.error('Something went wrong.');
                });
    }

    retry(_data, retrain) {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.monitorIngestedData(_data, retrain));
        return this.timerSubscription;
    }

    unsubscribe() {
        if (!this.coreUtilService.isNil(this.timerSubscription)) {
            this.timerSubscription.unsubscribe();
        }
        if (!this.coreUtilService.isNil(this.ingestStatusSubscription)) {
            this.ingestStatusSubscription.unsubscribe();
        }
        this._appUtilsService.loadingEnded();
    }

    ingestDataColumnNames(columnNamesDetails, retrain) {
        this.selectedIngestedColumnNames = [];
        const openIngestDataColumnClosed = this.dialogService.open(IngestDataColumnNamesComponent,
            {
                data: {
                    uniqueColmnNames: (this.isClustering == true) ? columnNamesDetails['ColumnNames'] : columnNamesDetails['UniqueColumns'],
                    columnNames: columnNamesDetails['ColumnNames'],
                    modelName: columnNamesDetails['ModelName'],
                    oldColumnNames: this.oldSelectedColumns,
                    filterAttribute: (columnNamesDetails['FilterAttribute'] && this.selectedServiceId !== 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') ? columnNamesDetails['FilterAttribute'] : undefined
                }
            }).afterClosed.pipe(
                tap(data => {
                    if (data instanceof Array) {
                        data.forEach(element => {
                            if (element.checked)
                                this.selectedIngestedColumnNames.push(element.name);
                        });
                        this._appUtilsService.loadingStarted();

                        let selectedUseCaseM = _.filter(this.trainedModels, function (item) {
                            return item.CorrelationId == columnNamesDetails.CorrelationId;
                        });

                        let dataSetUniqueId
                        if (selectedUseCaseM.length !== 0) {
                            dataSetUniqueId = selectedUseCaseM[0].DataSetUId
                        } else {
                            dataSetUniqueId = null;
                        }

                        // Train Ingested Data Model
                        // this.postSelectedColumnNamesModelTraining();
                        const payload = {
                            'CorrelationId': columnNamesDetails.CorrelationId,
                            'ClientId': columnNamesDetails.ClientId,
                            'DeliveryConstructId': columnNamesDetails.DeliveryconstructId,
                            'ServiceId': columnNamesDetails.ServiceId,
                            'UserId': columnNamesDetails.CreatedByUser,
                            'ModelName': columnNamesDetails.ModelName,
                            'CustomParam': { 'ReTrain': retrain, 'CorrelationId': '' },
                            'selectedColumns': this.selectedIngestedColumnNames,
                            'DataSetUId': dataSetUniqueId,
                            'MaxDataPull': this.provideTrainingDataVolume,
                            'IsCarryOutRetraining': this.selectedOfflineRetrain,
                            'IsOnline': this.IsOnline,
                            'IsOffline': this.IsOffline,
                            'Retraining': { [this.rFrequency]: this.selectedRFeqValues },
                            'Training': {},
                            'Prediction': {}
                        };
                        if (this.isClustering !== true) {
                            this.ingestStatusSubscription = this.problemStatementService.postSelectedColumnNamesModelTraining(payload)
                                .subscribe(
                                    data => {
                                        if (data) {
                                            this.getAICoreModels();
                                            if (data['IsSuccess'] == true) {
                                                this.disabledTrainSim = true;
                                                this.ns.success('Training Initiated.');
                                                this._appUtilsService.loadingEnded();
                                            }
                                            // this.ns.success(data.Message);
                                        }
                                    }, error => {
                                        this.ns.error('Error: Model Training Not Initiated');
                                    });
                        } else {
                            this.monitorIngestDataPayload = columnNamesDetails.CorrelationId;
                            this._appUtilsService.loadingEnded();
                            this.clusteringIngestData(false, this.monitorIngestDataPayload, this.selectedUniId);
                        }

                    } else if (data instanceof Object) {
                        data['columnNames'].forEach(element => {
                            if (element.checked)
                                this.selectedIngestedColumnNames.push(element.name);
                        });

                        let selectedUseCaseM = _.filter(this.trainedModels, function (item) {
                            return item.CorrelationId == columnNamesDetails.CorrelationId;
                        });

                        let dataSetUniqueId
                        if (selectedUseCaseM.length !== 0) {
                            dataSetUniqueId = selectedUseCaseM[0].DataSetUId
                        } else {
                            dataSetUniqueId = null;
                        }

                        // Train Ingested Data Model
                        // this.postSelectedColumnNamesModelTraining();
                        const payload = {
                            'CorrelationId': columnNamesDetails.CorrelationId,
                            'ClientId': columnNamesDetails.ClientId,
                            'DeliveryConstructId': columnNamesDetails.DeliveryconstructId,
                            'ServiceId': columnNamesDetails.ServiceId,
                            'UserId': columnNamesDetails.CreatedByUser,
                            'ModelName': columnNamesDetails.ModelName,
                            'CustomParam': { 'ReTrain': retrain, 'CorrelationId': '' },
                            'selectedColumns': this.selectedIngestedColumnNames,
                            'FilterAttribute': JSON.stringify(data['filterAttribute']),
                            'ScoreUniqueName': data['uniqueIdentifier'],
                            'Threshold_TopnRecords': JSON.stringify(data['Threshold_TopnRecords']),
                            'StopWords': JSON.stringify(data['StopWords']),
                            'DataSetUId': dataSetUniqueId,
                            'MaxDataPull': this.provideTrainingDataVolume,
                            'IsCarryOutRetraining': this.selectedOfflineRetrain,
                            'IsOnline': this.IsOnline,
                            'IsOffline': this.IsOffline,
                            'Retraining': { [this.rFrequency]: this.selectedRFeqValues },
                            'Training': {},
                            'Prediction': {}
                        };
                        if (this.isClustering !== true) {
                            this.ingestStatusSubscription = this.problemStatementService.postSelectedColumnNamesModelTraining(payload)
                                .subscribe(
                                    data => {
                                        if (data) {
                                            this.getAICoreModels();
                                            if (data['IsSuccess'] == true) {
                                                this.disabledTrainSim = true;
                                                this.ns.success('Training Initiated.');
                                            }
                                            // this.ns.success(data.Message);
                                        }
                                    }, error => {
                                        this.ns.error('Error: Model Training Not Initiated');
                                    });
                        } else {
                            this.monitorIngestDataPayload = columnNamesDetails.CorrelationId;
                            this.clusteringIngestData(false, this.monitorIngestDataPayload, this.selectedUniId);
                        }

                    } else {
                        this.ns.error('No columns selected.');
                    }
                    // console.log('Ingest Data Selected Column Names: ' + this.selectedIngestedColumnNames);
                })
            );
        openIngestDataColumnClosed.subscribe();
    }

    getDynamicEntity(clientUId) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.getDynamicEntity(clientUId, this.payloadForGetAICoreModels.dcid,
            this.payloadForGetAICoreModels.userid).subscribe(
                data => {
                    this._appUtilsService.loadingEnded();
                    this.entityData = data;
                    // console.log(this.entityData);
                },
                error => { this._appUtilsService.loadingEnded(); this.ns.error('Pheonix entity api has no data.'); });
    }

    reTrainSimModel(modelCorrelationId?) {
        if (!this.checkCorrelationId(modelCorrelationId)) {
            return false;
        } else {
            this._appUtilsService.loadingStarted();
            this.problemStatementService.getSimRetrainModelDetails(this.selectedModelCorrelationId).subscribe(
                data => {
                    this._appUtilsService.loadingEnded();
                    this.reTrainModel = true;
                    data['CorrelationId'] = this.selectedModelCorrelationId;
                    this.selectedModelData = data;
                    if (data.Source === 'Entity') {
                        if (this.entityView == false) {
                            this.setEntityView();
                        }
                        let retrainModelDetails = (data?.pad) ? JSON.parse(data.pad) : null;
                        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
                            this.fillInputFields(data.Custom);
                        }
                        else {
                            if (!this.coreUtilService.isNil(retrainModelDetails)) {
                                let entitiesForRetrain = Object.entries(retrainModelDetails.Entities);

                                let selectedEntityObj = { 'name': entitiesForRetrain[0][0], 'sub': entitiesForRetrain[0][1] };

                                this.entityArray[0] = selectedEntityObj;
                                console.log('Retraining - selected Entity Object', this.entityArray[0]);
                                this.selectedDeliveryType = retrainModelDetails.method;
                                this.onChangeOfDeliveryType(this.selectedDeliveryType);
                                this.selectedEntity = Object.keys(retrainModelDetails.Entities)[0];

                                this.entityFormGroup.get('startDateControl').enable();
                                this.entityFormGroup.get('endDateControl').enable();
                                this.entityFormGroup.patchValue({ startDateControl: new Date(retrainModelDetails.startDate), endDateControl: new Date(retrainModelDetails.endDate), entityControl: this.selectedEntity });
                            }
                        }
                    }

                    // this.selectedDeliveryType = data.
                    //this.reTrainModel = true;
                    this.ns.warning('Please upload your data to retrain the model');
                    window.scroll(0, 0);
                },
                error => { this._appUtilsService.loadingEnded(); this.ns.error('Error: Retrain Model Unsuccessful.'); });
        }
        return false;
    }

    trainRetrain(isRetrain: boolean, modelCorrelationId?) {
        if (!this.checkCorrelationId(modelCorrelationId)) {
            return false;
        } else {
            let correlationId = '';
            let uniId = '';
            const reTrainFlag = isRetrain;
            this.reTrainModel = isRetrain;
            if (isRetrain === false) {
                correlationId = '';
                if (this.isClustering === true) {
                    if ((this.numberOfCluster < 2 || this.numberOfCluster > 15) && this.selectedOptions['KMeans']['Auto_Cluster'] === 'False' && this.selectedOptions['KMeans']['Train_model'] === 'True') {
                        this.ns.error('Number of clusters should not be greater than 15 and less than 2.');
                    } else if (this.selectedOptions.KMeans.Train_model === 'False' && this.selectedOptions.DBSCAN.Train_model === 'False') {
                        this.ns.error('Kindly select atleast one Model Type');
                    } else {
                        this.showModelTemplateName(reTrainFlag, correlationId);
                    }
                } else {
                    this.showModelTemplateName(reTrainFlag, correlationId);
                }
            }
            if (isRetrain === true) {
                if (this.uploadedData.length === 0 && this.isClustering !== true) {
                    // this.ns.warning('Upload file to Retrain model.');
                    this.ns.warning('Please upload your data to retrain the model');
                    window.scroll(0, 0);
                } else if (this.modelSelectedForEvaluate === false) {
                    this.ns.warning('Kindly select a model to re-train.');
                } else {
                    if (this.isClustering === true) {
                        this.selectedNgramVal = [this.ngramOne, this.ngramTwo];
                        const isValueChanged = this.changeDetectionForTrainModel(this.oldProblemType, this.oldSelectedOptions);
                        if ((this.numberOfCluster < 2 || this.numberOfCluster > 15) && this.selectedOptions['KMeans']['Auto_Cluster'] === 'False' && this.selectedOptions['KMeans']['Train_model'] === 'True') {
                            this.ns.error('Number of clusters should not be greater than 15 and less than 2.');
                        } else if (this.selectedOptions.KMeans.Train_model === 'False' && this.selectedOptions.DBSCAN.Train_model === 'False') {
                            this.ns.error('Kindly select atleast one Model Type');
                        } else {
                            if (isValueChanged !== true) {
                                this.ns.error('Kindly change the model parameters to re-train the model.');
                            } else {
                                correlationId = this.selectedModelCorrelationId;
                                uniId = this.selectedUniId;
                                this.clusterMapping = { 'ClusterInputs': { 'KMeans': {}, 'DBSCAN': {} } };
                                this.clusteringIngestData(reTrainFlag, correlationId, uniId);
                            }
                        }
                    } else {
                        correlationId = this.selectedModelCorrelationId;
                        this.invokeAICoreServiceWithFile(reTrainFlag, correlationId);
                    }
                }
            }
        }
        // return false;
    }

    invokeAICoreServiceWithFile(reTrainFlag: boolean, correlationId: string) {
        this._appUtilsService.loadingStarted();
        const requestPayload = {
            'ClientId': this.paramData.clientUID,
            'DeliveryConstructId': this.paramData.deliveryConstructUId,
            'ServiceId': this.selectedServiceId,
            'UserId': this._appUtilsService.getCookies().UserId,
            'ModelName': this.enteredModelName.trim(),
            'CustomParam': { 'ReTrain': reTrainFlag, 'CorrelationId': correlationId },
            'file': this.uploadedData
        };
        this.problemStatementService.invokeAICoreServiceWithFile(this.uploadedData, requestPayload).subscribe(res => {
            if (res.hasOwnProperty('IsSuccess')) {
                if (res['IsSuccess'] === true) {
                    this._appUtilsService.loadingEnded();
                    this.ns.success(res['Message']);
                    this.uploadedData = [];
                    /* this.disabledRetrain = true;
                    this.disabledTrain = true;
                    this.rawEvaluatedData = undefined;
                    this.evaluatedResponse = undefined;
                    this.textForEvaluation = ''; */
                    // Populate AI core models
                    this.getAICoreModels();
                } else {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                    this.disabledRetrain = false;
                    this.uploadedData = [];
                    this.rawEvaluatedData = undefined;
                    this.evaluatedResponse = undefined;
                    this.textForEvaluation = '';
                }
            }
        }, (error) => {
            this._appUtilsService.loadingEnded();
            if (error.hasOwnProperty('error')) {
                this.ns.error(error.error);
            } else {
                this.ns.error(error);
            }
            this.uploadedData = [];
            this.rawEvaluatedData = undefined;
            this.evaluatedResponse = undefined;
        });
    }

    selectModel(model, modelTypeSelection?, event?) {
        // if (model.ModelStatus === 'Ready') {
        if (event !== undefined) {
            if (event.target.parentElement.firstElementChild.firstElementChild !== null) {
                event.target.parentElement.firstElementChild.firstElementChild.checked = true;
            }
        }

        if (this.backUpClusteringModels) {
            if (this.backUpClusteringModels.VisulalisationDatas.length) {
                this.backUpClusteringModels.VisulalisationDatas.forEach(data => {
                    if (data.CorrelationId == model.CorrelationId) {
                        model.visualisationData = data;
                    }
                });
            }
            this.backUpClusteringModels.clusteringStatus.forEach(data => {
                if (data.ingestData && data.CorrelationId == model.CorrelationId && !this.coreUtilService.isNil(data.ingestData.mapping)) {
                    model.mapping = data.ingestData.mapping;
                }
            });

        }

        //this.Bulkmultiple = 'Single';
        this.clusterChartTextView = false;
        this.disableDownload = true;
        this.clusterChartNonTextView = false;
        this.selectedModelCorrelationId = model.CorrelationId;
        this.selectedUniId = model.UniId;
        this.modelSelectedForEvaluate = true;
        this.enteredModelName = model.ModelName;
        this.selectedModelStatus = model.ModelStatus;
        if (this.isClustering !== true) {
            this.selectedIngestedColumnNames = [];
            this.oldSelectedColumns = [];
            this.selectedDataSource = '';
            this.disabledReTrainSim = true;
            this.disabledTrainSim = true;
            this.disabledEvaluate = true;
            // this.disabledGenerateWC = true;

            this.rangeValue = '';
            this.rangeValuemax = '';
            const columnsForEval = this.elem.nativeElement.querySelectorAll('.service-columns');
            columnsForEval.forEach(inputEl => {
                inputEl.value = '';
            });

            if (!this.coreUtilService.isNil(this.trainedModelColumns)) {
                this.trainedModelColumns.forEach(element => {
                    if (element.CorrelationId === this.selectedModelCorrelationId) {
                        this.selectedIngestedColumnNames = element['SelectedColumnNames'];
                        this.oldSelectedColumns = element['SelectedColumnNames'];
                        this.selectedDataSource = element['DataSource'];
                        this.uniqueScore = element['ScoreUniqueName'];
                        this.selectedbulk_multiple = false;
                        if (this.selectedServiceId === 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') { // Next word Prediction
                            this.columnSelectValForEvaluate = undefined;
                        }
                    }
                });
            }

            if (model.ModelStatus === 'Completed') {
                this.disabledReTrainSim = false;
                this.disabledTrainSim = true;
            } else if (this.selectedModelStatus === 'Training is not initiated') {
                this.Bulkmultiple = 'Single';
                //else if (this.selectedModelStatus === 'Upload InProgress' || this.selectedModelStatus === 'Training is not initiated') {
                this.disabledTrainSim = false;
                this.modelIsSelected = true;
            } else {
                this.disabledReTrainSim = true;
                this.Bulkmultiple = 'Single';
            }
        }
        this.evaluatedResponse = undefined;
        this.rawEvaluatedData = undefined;
        this.textForEvaluation = '';
        if (this.isClustering === true) {

            this.disableDwnldMappedData = true;
            this.disabledGenerateWC = true;
            this.clusterMapping = { 'ClusterInputs': { 'KMeans': {}, 'DBSCAN': {} } };
            this.resetClusteringEvalData();
            this.uploadedData = [];
            //this.originalData = [];
            //  this.fileInput.nativeElement.value = '';
            if (!this.coreUtilService.isNil(model.ingestData)) {
                if (!this.coreUtilService.isNil(model.ingestData.ProblemType)) {
                    this.oldProblemType = model.ingestData.ProblemType;
                }

                if (!this.coreUtilService.isNil(model.ingestData.DataSetUId)) {
                    this.selectDatasetUI = model.ingestData.DataSetUId;
                }

                if (!this.coreUtilService.isNil(model.ingestData.SelectedModels)) {
                    this.oldSelectedOptions = model.ingestData.SelectedModels;
                }

                if (!this.coreUtilService.isNil(model.ingestData.StopWords)) {
                    this.oldSeletedStopWords = model.ingestData.StopWords;
                }

                if (!this.coreUtilService.isNil(model.ingestData.Ngram)) {
                    this.oldNgramSelection = model.ingestData.Ngram;
                }

                if (!this.coreUtilService.isNil(model.ingestData.Columnsselectedbyuser)) {
                    this.selectedIngestedColumnNames = model.ingestData.Columnsselectedbyuser;
                    //this.originalData = model.ingestData.Columnsselectedbyuser;
                }
            }
            if (!this.coreUtilService.isNil(model.Clusters)) {
                this.selectedClusterValue = model.Clusters;
            }
            if (model.pageInfo === 'InvokeIngestData' && model.ModelStatus === 'Completed') {
                this.disabledTrain = false;
                this.disabledRetrain = true;
                this.disabledTextNontext = false;
            } else {
                this.disabledTrain = true;
                this.disabledTextNontext = true;
                const columnsForEvaluate = this.columnsForEvaluate.find(obj => obj.CorrelationId === this.selectedModelCorrelationId);
                if (columnsForEvaluate !== undefined) {
                    if (columnsForEvaluate.hasOwnProperty('Text_columns')) {
                        this.textColumns = columnsForEvaluate.Text_columns;
                    }
                    if (columnsForEvaluate.hasOwnProperty('Numerical_columns')) {
                        this.nonTextColumns = columnsForEvaluate.Numerical_columns;
                    }
                    if (columnsForEvaluate.hasOwnProperty('Category_columns')) {
                        this.categoryColumns = columnsForEvaluate.Category_columns;
                    }
                    this.modelTypeForEval = modelTypeSelection;
                    if (this.modelTypeForEval === 'KMeans') {
                        this.selectedModelsForEval.KMeans.Train_model = 'True';
                        this.selectedModelsForEval.DBSCAN.Train_model = 'False';
                        this.selectedModelsForEval.Agglomerative.Train_model = 'False';
                    } else if (this.modelTypeForEval === 'DBSCAN') {
                        this.selectedModelsForEval.KMeans.Train_model = 'False';
                        this.selectedModelsForEval.DBSCAN.Train_model = 'True';
                        this.selectedModelsForEval.Agglomerative.Train_model = 'False';
                    } else {
                        this.selectedModelsForEval.KMeans.Train_model = 'False';
                        this.selectedModelsForEval.DBSCAN.Train_model = 'False';
                        this.selectedModelsForEval.Agglomerative.Train_model = 'True';
                    }
                    if (columnsForEvaluate.Clustering_type.Text === 'True') {
                        this.clusteringType = 'Text';
                        this.totalCluster = Object.keys(model.Suggestion).length;
                        this.autoFillClusterData(this.totalCluster, model.Suggestion, modelTypeSelection, model);
                    } else {
                        this.clusteringType = 'Non-Text';
                    }
                }
                // Bug Fix for 1078054
                // else {
                //   this.ns.error('Training is not completed yet.');
                // }
            }
        }
        //  this.disabledEvaluate = false;
        // }
        if (model.ModelStatus === 'Completed' || model.ModelStatus === 'Ready' || model.ModelStatus === 'Task Complete') {
            this.disabledEvaluate = false;
            this.disabledRetrain = false;
            if (this.isClustering === true) {
                if (model.pageInfo === 'InvokeIngestData' && model.ModelStatus === 'Completed') {
                    this.disabledTrain = false;
                    this.disabledRetrain = true;
                    this.disabledEvaluate = true;
                }
            }

            if (this.isWordCloud == true) {
                if (!this.coreUtilService.isNil(model.ingestData.Columnsselectedbyuser)) {
                    this.originalData = model.ingestData.Columnsselectedbyuser;
                    this.originalDataCheck = Object.assign({}, this.originalData);
                }
            }
            if (this.isWordCloud === true) { // Word Cloud
                this.modelStatusValue = model.ModelStatus;
                this.ingestDataColumnValue = model.ingestData.Columnsselectedbyuser;
                if (this.modelStatusValue === 'Completed') {

                    if (this.ingestDataColumnValue !== null) {
                        this.wordCloudValue = model.ingestData.Columnsselectedbyuser;
                        // if (model.ingestData.Columnsselectedbyuser.length > 0) {
                        if (this.wordCloudValue.length > 0) {
                            //this.disabledGenerateWC = false;
                            this.disabledGenerateWC = true;
                        } else {
                            this.disabledGenerateWC = false;
                        }
                    } else {
                        //this.disabledGenerateWC = true;
                        this.disabledGenerateWC = true;
                    }
                    model.StopWords = [];
                    if (model.ingestData.Columnsselectedbyuser == null) {
                        model.ingestData.Columnsselectedbyuser = [];
                    }
                    model.unSelectedColumnNames = [];
                    model.unSelectedColumnNames = model.ColumnNames.filter(item => !model.ingestData.Columnsselectedbyuser.includes(item));

                    if (model.unSelectedColumnNames.length === 0) {
                        this.toggleSelect = false;
                    } else {
                        this.toggleSelect = true;
                    }

                    this.stopWordsWC = '';
                    this.selectedWCImage = '';
                }
            }
        } else if (model.ModelStatus === 'InProgress' || model.ModelStatus === 'In - Progress' || model.ModelStatus === 'Upload InProgress' || model.ModelStatus === 'Progress') {
            this.disabledEvaluate = true;
            this.disabledRetrain = true;
            this.disabledTrain = true;
            this.disabledReTrainSim = true;
            this.Bulkmultiple = 'Single';
            this.ns.warning(model.StatusMessage);
        } else if (model.ModelStatus === 'Error' || model.ModelStatus === 'Warning') {
            this.disabledEvaluate = true;
            this.disabledRetrain = true;
            this.ns.error(model.StatusMessage);
            this.Bulkmultiple = 'Single';
        } else if (model.ModelStatus === 'ReTrain Failed') {
            this.disabledEvaluate = false;
            this.disabledRetrain = false;
            this.ns.error(model.StatusMessage);
        }
        if (model?.visualisationData && Object.keys(model?.visualisationData).length) {
            this.disableDwnldMappedData = false;
            if (model.Clustering_type == 'Text') {
                this.clusterChartTextView = true;
                this.clusterChartNonTextView = false;
                this.graphicalTextVisualizationView(model.visualisationData);
            } else {
                this.clusterChartTextView = false;
                this.clusterChartNonTextView = true;
                this.disableDwnldMappedData = false;
                this.graphicalNonTextVisualizationView(model.visualisationData);
            }
        } else if (model?.mapping && !Object.keys(model?.mapping).length) {
            this.disableDwnldMappedData = true;
        }
    }

    evaluate() {
        let param;
        let bodyParam;
        let maxSentenceValid = true;
        if (this.templateFlag === '1') {
            if (this.selectedModelCorrelationId === '' || this.selectedModelCorrelationId === undefined) {
                this.ns.error('Kindly select a model to evaluate.');
            } else if (this.textForEvaluation.length === 0 && this.isClustering === false &&
                this.similarityAnalysis === false && this.isDeveloperPrediction === false) {
                this.ns.warning('Provide the text to evaluate.');
            } else if (this.selectedModelStatus === 'InProgress' || this.selectedModelStatus === 'Error') {
                this.ns.warning('Only trained model will be evaluated.');
            } else {
                param = {
                    'correlationid': this.selectedModelCorrelationId
                };
                let inputEmpty = false;
                //  let isSpecialChar = false;
                if (this.selectedServiceId !== '50f2232a-0182-4cda-96fc-df8f3ccd216c') { // except intent and entity
                    const objForEval = {};
                    const dataForEval = [];
                    const columnsForEval = this.elem.nativeElement.querySelectorAll('.service-columns');

                    if (this.selectedServiceId === 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') { // Next word Prediction
                        if (this.coreUtilService.isNil(this.rangeValue)) {
                            maxSentenceValid = false;
                            this.ns.error('Kindly provide the number of sentences.');
                        } else if (this.rangeValue <= 0) {
                            maxSentenceValid = false;
                            this.ns.error('Please re-adjust the maximum number of sentence and try again.');
                        } else if (this.rangeValue > 500) { // BugFix 798451
                            maxSentenceValid = false;
                            this.ns.error('Maximum number of sentence must be below 500. Please re-adjust and try again.');
                        } else {
                            columnsForEval.forEach(inputEl => {
                                /* if ((this.isSpecialCharacter(inputEl.value)) === 0) {
                                  isSpecialChar = true;
                                } */
                                objForEval[this.columnSelectValForEvaluate] = inputEl.value;
                                if (this.coreUtilService.isNil(inputEl.value.trim())) {
                                    inputEmpty = true;
                                }
                            });
                            objForEval['NWords'] = this.rangeValue;
                            dataForEval.push(objForEval);
                            /* if (isSpecialChar) {
                              this.ns.warning('No special characters allowed.');
                              return 0;
                            } */
                            param = {
                                'CorrelationId': this.selectedModelCorrelationId,
                                'UserId': this._appUtilsService.getCookies().UserId,
                                'DCUID': this.paramData.deliveryConstructUId,
                                'ClientID': this.paramData.clientUID,
                                'UniId': '',
                                'CreatedDate': new Date(),
                                'Data': dataForEval
                            };
                        }
                    } else {
                        if (this.selectedServiceId === '93df37dc-cc72-4105-9ad2-fd08509bc823' && this.Bulkmultiple === 'Bulk') {

                        } else {
                            inputEmpty = true;
                            columnsForEval.forEach(inputEl => {
                                const keyValue = inputEl.dataset['columnval'];
                                /* if ((this.isSpecialCharacter(inputEl.value)) === 0) {
                                  isSpecialChar = true;
                                } */
                                objForEval[keyValue] = inputEl.value;
                                if (!this.coreUtilService.isNil(inputEl.value.trim())) {
                                    inputEmpty = false;
                                }
                            });
                            dataForEval.push(objForEval);
                        }
                        /* if (isSpecialChar) {
                          this.ns.warning('No special characters allowed.');
                          return 0;
                        } */
                        if (this.selectedServiceId === '93df37dc-cc72-4105-9ad2-fd08509bc823') {
                            if (this.Bulkmultiple === 'Single') {
                                param = {
                                    'CorrelationId': this.selectedModelCorrelationId,
                                    'UserId': this._appUtilsService.getCookies().UserId,
                                    'DCUID': this.paramData.deliveryConstructUId,
                                    'ClientID': this.paramData.clientUID,
                                    'UniId': '',
                                    'CreatedDate': new Date(),
                                    'Data': dataForEval,
                                    'Bulk': 'Single'
                                };
                            } else {
                                param = {
                                    'CorrelationId': this.selectedModelCorrelationId,
                                    'UserId': this._appUtilsService.getCookies().UserId,
                                    'DCUID': this.paramData.deliveryConstructUId,
                                    'ClientID': this.paramData.clientUID,
                                    'UniId': '',
                                    'CreatedDate': new Date(),
                                    'Data': [],
                                    'Bulk': 'Bulk'
                                };
                            }
                        } else {
                            param = {
                                'CorrelationId': this.selectedModelCorrelationId,
                                'UserId': this._appUtilsService.getCookies().UserId,
                                'DCUID': this.paramData.deliveryConstructUId,
                                'ClientID': this.paramData.clientUID,
                                'UniId': '',
                                'CreatedDate': new Date(),
                                'Data': dataForEval
                            };
                        }
                    }
                    // BugFix 798451 
                    // dataForEval.push(objForEval);
                    // if (isSpecialChar) {
                    //   this.ns.warning('No special characters allowed.');
                    //   return 0;
                    // }
                    // param = {
                    //   'CorrelationId': this.selectedModelCorrelationId,
                    //   'UserId': this._appUtilsService.getCookies().UserId,
                    //   'DCUID': this.paramData.deliveryConstructUId,
                    //   'ClientID': this.paramData.clientUID,
                    //   'UniId': '',
                    //   'CreatedDate': new Date(),
                    //   'Data': dataForEval
                    // };
                } else {
                    bodyParam = {
                        'Payload': { 'query': this.textForEvaluation }
                    };
                }
                if (this.isClustering !== true) {
                    if (inputEmpty === false) {
                        if (maxSentenceValid === true) // BugFix 798451
                            this.evaluateWithFile(bodyParam, param);
                    } else if (this.selectedServiceId !== 'e6f8243b-e00f-4d05-b16c-8c6a449b5a4c') { // Except Next word Prediction
                        this.ns.error('Kindly enter value atleast for a text box.');
                    } else {
                        this.ns.error('Kindly enter the value for all text boxes.');
                    }
                } else {
                    this.evaluateForClustering();
                }
            }
        } else if (this.templateFlag === '2') {
            if (this.coreUtilService.isNil(this.textForEvaluation)) {
                this.ns.error('Provide the text to evaluate.');
            } else {
                bodyParam = {
                    'ClientID': this.paramData.clientUID,
                    'DeliveryConstructID': this.paramData.deliveryConstructUId,
                    'UserID': this._appUtilsService.getCookies().UserId,
                    'ServiceID': this.selectedServiceId,
                    'Payload': this.selectedServiceId != '6356c856-0125-4409-b147-5fc4d48547ae' ? { 'text': this.textForEvaluation } : { 'text': this.textForEvaluation, 'language': this.selectedLangTranslation }
                };
                this.evaluateWithoutFile(bodyParam);
            }
        } else if (this.templateFlag === '3') {
            if (this.coreUtilService.isNil(this.textForEvaluation)) {
                this.ns.error('Provide the text to evaluate.');
            } else if (this.coreUtilService.isNil(this.rangeValue) || this.coreUtilService.isNil(this.rangeValuemax)) {
                this.ns.error('Kindly provide both minimum and maximum number of words.');
            } else if (this.rangeValue <= 0) { // BugFix 798451
                this.ns.error('Please re-adjust the number of words and try again.');
            } else if (this.rangeValue >= this.rangeValuemax) {
                this.ns.error('Maximum should always be greater than minimum.');
            } else if (this.rangeValuemax >= 500) { // BugFix 798451
                this.ns.error('Maximum number of words must be below 500. Please re-adjust and try again.');
            } else {
                bodyParam = {
                    'ClientID': this.paramData.clientUID,
                    'DeliveryConstructID': this.paramData.deliveryConstructUId,
                    'ServiceID': this.selectedServiceId,
                    'UserID': this._appUtilsService.getCookies().UserId,
                    'Payload': { 'query': this.textForEvaluation, 'minrange': this.rangeValue, 'maxrange': this.rangeValuemax }
                };
                this.evaluateWithoutFile(bodyParam);
            }
        }
    }

    convertConfidence(result) {
        if (result.entities.length > 0) {
            result.entities.forEach(element => {
                if (element.hasOwnProperty('confidence')) {
                    element.confidence = element.confidence * 100;
                    element.confidence = element.confidence.toFixed(1);
                }
            });
        }
        if (result.intent.hasOwnProperty('confidence')) {
            result.intent.confidence = result.intent.confidence * 100;
            result.intent.confidence = result.intent.confidence.toFixed(1);
        }
        this.evaluatedResponse = result;
    }

    showModelTemplateName(reTrainFlag: boolean, correlationId: string) {
        const openTemplateAfterClosed =
            this.dialogService.open(TemplateNameModalComponent,
                { data: { title: 'Enter Model Name', pageInfo: 'CreateAICoreModel' } }).afterClosed.pipe(
                    tap(modalName => {
                        if (modalName !== undefined) {
                            this.saveModelName(modalName, reTrainFlag, correlationId);
                        }
                    })
                );
        openTemplateAfterClosed.subscribe();
    }

    saveModelName(name: string, reTrainFlag: boolean, correlationId: string) {
        this.enteredModelName = name;
        if (this.isClustering === true) {
            const uniId = '';
            this.clusteringIngestData(reTrainFlag, correlationId, uniId);
        } else {
            this.invokeAICoreServiceWithFile(reTrainFlag, correlationId);
        }
    }

    previous() {
        this.router.navigate(['choosefocusarea']);
        return false;
    }

    openResponse(responsetype) {
        if (responsetype === 'formattedResponse') {
            this.responseType = 'formatted';
        } else {
            this.responseType = 'raw';
        }
    }

    inputChange() {
        this.disabledEvaluate = false;
    }

    languageTranslationChange(language) {
        this.selectedLangTranslation = language;
    }

    evaluateWithFile(bodyParam, param) {
        this._appUtilsService.loadingStarted();
        this.evalPredictionHeaders = [];
        this.problemStatementService.evaluate(bodyParam, param, this.selectedServiceId, this.decimalPoint).subscribe(response => {
            if (response.hasOwnProperty('IsSuccess')) {
                if (response.IsSuccess === true) {
                    this._appUtilsService.loadingEnded();
                    this.disabledTrain = true;
                    this.disabledRetrain = true;
                    this.uploadedData = [];
                    this.rawEvaluatedData = response;
                    if (response.ReturnValue.ResponseData.hasOwnProperty('result')) {
                        const result = response.ReturnValue.ResponseData.result;
                        this.convertConfidence(result);
                    } else if (response.ReturnValue.ResponseData.hasOwnProperty('response')) {
                        this.evaluatedResponse = response.ReturnValue.ResponseData;
                    } else if (response.ReturnValue.ResponseData.hasOwnProperty('prediction_list')) {
                        this.evaluatedResponse = response.ReturnValue.ResponseData;
                    } else if (response.ReturnValue.ResponseData.hasOwnProperty('Predictions')) {
                        if (this.Bulkmultiple === 'Bulk') {
                            this.ns.warning('Prediction has been initiated. Please wait for the Prediction Response');
                            this.getSAMultipleBulkPredictionStatus();
                        }
                        this.evaluatedResponse = response.ReturnValue.ResponseData;
                        if (response.ReturnValue.ResponseData['Predictions'].length === 1) {
                            if (response.ReturnValue.ResponseData['Predictions'][0].score === '' || response.ReturnValue.ResponseData['Predictions'][0].score == undefined) {
                                if (response.ReturnValue.ResponseData['Predictions'][0].includes('There are no similarity predictions for the threshold')) {
                                    this.ns.warning(response.ReturnValue.ResponseData['Predictions'][0]);
                                }
                            } else {
                                if (this.evaluatedResponse['Predictions'].length === 1) {
                                    this.evalPredictionHeaders = (this.evaluatedResponse['Predictions'].length > 0) ? Object.keys(this.evaluatedResponse['Predictions'][0]) : [];
                                } else {
                                    this.evalPredictionHeaders = (this.evaluatedResponse['Predictions'].length > 0) ? Object.keys(this.evaluatedResponse['Predictions'][1]) : [];
                                }
                            }
                        } else {
                            this.evalPredictionHeaders = (this.evaluatedResponse['Predictions'].length > 0) ? Object.keys(this.evaluatedResponse['Predictions'][1]) : [];
                        }
                    }
                    if (response.ReturnValue.ResponseData.hasOwnProperty('is_success')) {
                        if (response.ReturnValue.ResponseData.is_success === false) {
                            this.ns.error(response.ReturnValue.ResponseData.message);
                        }
                    }
                    if (response.ReturnValue.ResponseData.hasOwnProperty('status')) {
                        if (response.ReturnValue.ResponseData.status === 'False' || response.ReturnValue.ResponseData.status === 'false') {
                            this.ns.error(response.ReturnValue.ResponseData.message);
                            this.disableDownload = true;
                        }
                    }

                } else {
                    this._appUtilsService.loadingEnded();
                    if (response.hasOwnProperty('Message')) {
                        this.ns.error(response.Message);
                    }
                }
            }
        }, error => {
            this._appUtilsService.loadingEnded();
            this.ns.error('Something went wrong.');
        });
    }

    evaluateWithoutFile(bodyParam) {
        this._appUtilsService.loadingStarted();
        if (this.selectedServiceId != '6356c856-0125-4409-b147-5fc4d48547ae') {
            if (this.selectedServiceId === '6b176706-5972-4801-90a0-54af0a5c7490') {
                this.problemStatementService.getTextSummary(bodyParam).subscribe(response => {
                    if (response.hasOwnProperty('IsSuccess')) {
                        if (response.ReturnValue.ResponseData.status == 'True') {
                            this._appUtilsService.loadingEnded();
                            this.rawEvaluatedData = response;
                            const rCorrelationID = response.ReturnValue.CorrelationId;
                            this.getEvaluateResponse(rCorrelationID);
                        } else {
                            this._appUtilsService.loadingEnded();
                            this.ns.error(response.ReturnValue.ResponseData.message);

                        }
                    }
                }, error => {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                });
            } else {
                this.problemStatementService.invokeAICoreService(bodyParam).subscribe(response => {
                    if (response.hasOwnProperty('IsSuccess')) {
                        if (response.IsSuccess === true) {
                            this._appUtilsService.loadingEnded();
                            this.rawEvaluatedData = response;
                            if (response.ReturnValue.ResponseData.hasOwnProperty('response')) {
                                this.evaluatedResponse = response.ReturnValue.ResponseData.response;
                            }
                            if (response.ReturnValue.ResponseData.hasOwnProperty('response_data')) {
                                this.evaluatedResponse = response.ReturnValue.ResponseData.response_data[0];
                            }
                            if (response.ReturnValue.ResponseData.hasOwnProperty('is_success')) {
                                if (response.ReturnValue.ResponseData.is_success === false) {
                                    this.ns.error(response.ReturnValue.ResponseData.message);
                                }
                            }
                        } else {
                            this._appUtilsService.loadingEnded();
                            if (response.hasOwnProperty('Message')) {
                                this.ns.error(response.Message);
                            }
                        }
                    }
                }, error => {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                });
            }
        } else {
            if (this.selectedLangTranslation === '') {
                this._appUtilsService.loadingEnded();
                this.ns.error('Please Select Language for Translation');
            } else {
                this.problemStatementService.invokeLangTranslationService(bodyParam).subscribe(response => {
                    if (response.hasOwnProperty('IsSuccess')) {
                        if (response.IsSuccess === true) {
                            this._appUtilsService.loadingEnded();
                            this.rawEvaluatedData = response;
                            if (response.ReturnValue.ResponseData.hasOwnProperty('response_data')) {
                                this.evaluatedResponse = response.ReturnValue.ResponseData.response_data[0];
                            }
                            if (response.ReturnValue.ResponseData.hasOwnProperty('is_success')) {
                                if (response.ReturnValue.ResponseData.is_success === false) {
                                    this.ns.error(response.ReturnValue.ResponseData.message);
                                }
                            }
                        } else {
                            this._appUtilsService.loadingEnded();
                            if (response.hasOwnProperty('Message')) {
                                this.ns.error(response.Message);
                            }
                        }
                    }
                }, error => {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                });
            }

        }
    }

    getEvaluateResponse(correlationId) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.getTextSummaryStatus(correlationId).subscribe(response => {
            if (response) {
                if (response.Status === 'P' || response.Status === null) {
                    this.retryStatus(correlationId);
                } else if (response.Status === 'C') {
                    this._appUtilsService.loadingImmediateEnded();
                    this.problemStatementService.getTextSummaryResult(correlationId).subscribe(response => {
                        if (response.hasOwnProperty('IsSuccess')) {
                            this.rawEvaluatedData = response;
                            if (response.IsSuccess === true) {
                                if (response.ReturnValue.hasOwnProperty('ResponseData')) {
                                    this.evaluatedResponse = response.ReturnValue.ResponseData;
                                }
                            }
                        }
                    });
                } else if (response.Status === 'E') {
                    this._appUtilsService.loadingImmediateEnded();
                    this.ns.error(response.ErrorMessage);
                }
            }
        });
    }

    retryDownloadStatusForBulk() {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.getSAMultipleBulkPredictionStatus());
        return this.timerSubscription;
    }

    getSAMultipleBulkPredictionStatus() {
        this.problemStatementService.getSAMultipleBulkPredictionStatus(this.selectedModelCorrelationId, this.selectedUniId).subscribe(data => {
            if (data.Status == 'C') {
                this.ns.success('Prediction is completed. You can download the file now.')
                this.disableDownload = false;
            } else if (data.Status == 'E') {
                this.ns.error(data.ErrorMessage);
            } else if (data.Status == 'P') {
                this.retryDownloadStatusForBulk();
            }
        });
    }

    retryStatus(correlationId) {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.getEvaluateResponse(correlationId));
        return this.timerSubscription;
    }

    clusteringIngestData(reTrainFlag, correlationId, uniId) {
        this._appUtilsService.loadingStarted();
        if (this.selectedOptions.KMeans.Train_model === 'True' && this.selectedOptions.KMeans.Auto_Cluster === 'True') {
            this.selectedOptions.KMeans.No_of_Clusters = 1;
        }
        if (this.problemType['Clustering']['Non-Text'] === 'True') {
            this.ngramOne = 0;
            this.ngramTwo = 0;
        }

        let selectedUseCaseM = _.filter(this.trainedModels, function (item) {
            return item.CorrelationId == correlationId;
        });

        // let dataSetUniqueId
        // if (selectedUseCaseM.length !== 0) {
        //   dataSetUniqueId = selectedUseCaseM[0].DataSetUId
        // } else {
        //   dataSetUniqueId = null;
        // }

        const requestPayload = {
            'CorrelationId': correlationId,
            'PageInfo': (this.isClustering === true) ? 'InvokeIngestData' : 'wordcloud',
            'UserId': this._appUtilsService.getCookies().UserId,
            'DCUID': this.paramData.deliveryConstructUId,
            'ClientID': this.paramData.clientUID,
            'UniId': uniId,
            'ServiceID': this.selectedServiceId,
            /* 'ParamArgs': { 'Type': 'File', 'URL': '' }, */
            'CreatedDate': new Date(),
            'Token': '',
            'ProblemType': this.problemType,
            'SelectedModels': this.selectedOptions,
            'ModelName': this.enteredModelName,
            'retrain': reTrainFlag,
            /* 'file': this.uploadedData, */
            'selectedColumns': this.selectedIngestedColumnNames,
            'StopWords': this.clusteringSW,
            'Ngram': [this.ngramOne, this.ngramTwo],
            'DataSetUId': this.selectDatasetUI,
            'MaxDataPull': this.provideTrainingDataVolume,
            'IsCarryOutRetraining': this.selectedOfflineRetrain,
            'IsOnline': this.IsOnline,
            'IsOffline': this.IsOffline,
            'Retraining': { [this.rFrequency]: this.selectedRFeqValues },
            'Training': {},
            'Prediction': {}
        };
        this.problemStatementService.ClusteringIngestData(this.uploadedData, requestPayload).subscribe(res => {
            if (res.hasOwnProperty('IsSuccess')) {
                if (res['IsSuccess'] === true) {
                    this._appUtilsService.loadingEnded();
                    this.ns.success(res['Message']);
                    // Populate AI core models
                    this.getCusteringModels();
                } else {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                    this.disabledRetrain = false;
                    this.uploadedData = [];
                    this.rawEvaluatedData = undefined;
                    this.evaluatedResponse = undefined;
                    this.textForEvaluation = '';
                }
            }
        }, (error) => {
            this._appUtilsService.loadingEnded();
            if (error.hasOwnProperty('error')) {
                this.ns.error(error.error);
            } else {
                this.ns.error(error);
            }
            this.uploadedData = [];
            this.rawEvaluatedData = undefined;
            this.evaluatedResponse = undefined;
        });
        this.ngramOne = 2;
        this.ngramTwo = 3;
        this.clusteringSW = [];
    }

    onSwitchChanged(elementRef, modelName) {
        if (elementRef.checked === true) {
            this.selectedOptions[modelName].Train_model = 'True';
            if (modelName === 'DBSCAN') {
                this.ns.warning('If the number of noise points in the DBSCAN Algorithm are too large, there is a possibility of your data not getting assigned to a particular cluster.');
            }
        }
        if (elementRef.checked === false) {
            this.selectedOptions[modelName].Train_model = 'False';
        }
    }

    onProblemTypeChange(elementRef, modelName) {
        this.problemType[modelName]['Text'] = 'True';
    }

    onClusterChange(value, clusterInput?) {
        if (value === 'Auto') {
            this.selectedOptions['KMeans']['Auto_Cluster'] = 'True';
            this.selectedOptions['KMeans']['No_of_Clusters'] = 1;
        } else if (value === 'Cluster') {
            this.selectedOptions['KMeans']['Auto_Cluster'] = 'False';
            if (this.numberOfCluster === null) {
                this.numberOfCluster = 1;
            }
            this.selectedOptions['KMeans']['No_of_Clusters'] = this.numberOfCluster;
        } else {
            if (typeof clusterInput === 'string') {
                this.numberOfCluster = parseInt(clusterInput, 0);
            }
            if (this.numberOfCluster === null) {
                this.numberOfCluster = 2;
            }
            this.selectedOptions['KMeans']['No_of_Clusters'] = this.numberOfCluster;
        }
    }

    getCusteringModels() {
        if (this.reTrainModel) {
            this.setFileView();
            this.reTrainModel = false;
        }
        // if (refreshM === 'refreshM') {
        //   this.setFileView();
        // }

        this.disabledTextNontext = false;
        this._appUtilsService.loadingStarted();
        this.filteredTrainedModels = undefined;
        this.filterType = 'All';
        this.reset();
        const params = {
            'clientid': this.paramData.clientUID,
            'dcid': this.paramData.deliveryConstructUId,
            'serviceid': this.selectedServiceId,
            'userid': this._appUtilsService.getCookies().UserId
        };
        this.problemStatementService.getCusteringModels(params).subscribe(models => {
            this.pageloadCluster = _.cloneDeep(models);
            this._appUtilsService.loadingEnded();
            this.clusteredModels = models.clusteringStatus;
            this.visualisationData = models?.VisulalisationDatas;
            this.columnsForEvaluate = models.ClusteredColumns;
            const uniqueCorrelationIds = [];
            this.modelTypes = [];
            const filteredModels = [];
            let enteredIndex;
            let counter = 0;
            this.clusteredModels.forEach((element, i) => {
                const obj = {
                    'CorrelationId': '',
                    'modelTypes': [],
                    'ModelName': '',
                    'PredictionURL': '',
                    'ModifiedOn': '',
                    'ModelStatus': '',
                    'StatusMessage': '',
                    'selectedModelType': '',
                    'Clustering_type': '',
                    'pageInfo': '',
                    'ingestData': '',
                    'Silhouette_Coefficient': '',
                    'UniId': '',
                    'Clusters': 0,
                    'selectedModelInList': '',
                    'Suggestion': {},
                    'Columnsselectedbyuser': [],
                    'DataSource': '',
                    'ColumnNames': [], // For Word Cloud
                    'validcolumselect': false,
                    'StopWords': '',
                    'Ngram': '',
                    'DataSetUId': '',
                    'visualisationData': {},
                    'mapping': {},
                    'Status': ''
                };

                if (uniqueCorrelationIds.indexOf(element.CorrelationId) < 0) {
                    enteredIndex = i;
                    uniqueCorrelationIds.push(element.CorrelationId);
                    obj.CorrelationId = element.CorrelationId;
                    obj.modelTypes.push(element.ModelType);
                    obj.selectedModelType = element.ModelType;
                    obj.ModelName = element.ModelName;
                    obj.PredictionURL = element.PredictionURL;
                    obj.ModifiedOn = element.ModifiedOn;
                    obj.ModelStatus = element.RequestStatus;
                    obj.StatusMessage = element.Message;
                    obj.Clustering_type = element.Clustering_type;
                    obj.pageInfo = element.pageInfo;
                    obj.ingestData = element.ingestData;
                    obj.Silhouette_Coefficient = element.Silhouette_Coefficient;
                    obj.UniId = element.UniId;
                    obj.Clusters = element.Clusters;
                    // obj.validcolumselect = element.ingestData.ValidColumnsSelected;
                    obj.DataSource = '';
                    obj.Status = element.Status;
                    if (!this.coreUtilService.isNil(element.Suggestion)) {
                        obj.Suggestion = element.Suggestion;
                    }
                    obj.selectedModelInList = element.ModelType;
                    if (!this.coreUtilService.isNil(element.ingestData)) {
                        if (!this.coreUtilService.isNil(element.ingestData.Columnsselectedbyuser)) {
                            obj.Columnsselectedbyuser = element.ingestData.Columnsselectedbyuser;
                            this.selectedIngestedColumnNames = element.ingestData.Columnsselectedbyuser;
                            obj.DataSetUId = element.ingestData.DataSetUId;
                        }
                        if (!this.coreUtilService.isNil(element.ingestData.DataSource)) {
                            obj.DataSource = element.ingestData.DataSource;
                        }
                        if (!this.coreUtilService.isNil(element.ingestData.StopWords)) {
                            obj.StopWords = element.ingestData.StopWords;
                        }
                        if (!this.coreUtilService.isNil(element.ingestData.mapping)) {
                            obj.mapping = element.ingestData.mapping;
                        }
                        if (!this.coreUtilService.isNil(element.ingestData.Ngram)) {
                            obj.Ngram = element.ingestData.Ngram;
                            if (obj.Ngram[0].toString() === '0' && obj.Ngram[1].toString() == '0') {
                                obj.Ngram = null;
                            }
                        }
                    }

                    // if (!this.coreUtilService.isNil(element.ingestData.StopWords)) {
                    // obj.StopWords = element.ingestData.StopWords;
                    // }

                    if (this.isWordCloud === true) { // For Word Cloud
                        obj.validcolumselect = element.ingestData?.ValidColumnsSelected;
                        obj.ColumnNames = element.ColumnNames;
                    }

                    if (this.visualisationData.length) {
                        this.visualisationData.forEach(data => {
                            if (data.CorrelationId == element.CorrelationId) {
                                obj.visualisationData = data;
                            }
                        });
                    }

                    filteredModels.push(obj);
                } else {
                    filteredModels[enteredIndex - counter].modelTypes.push(element.ModelType);
                    counter++;
                }
            });

            this.filteredTrainedModels = filteredModels;
            if (models.length === 0) {
                this.trainedModels = undefined;
            } else {
                if (this.filterType === 'All') {
                    this.trainedModels = this.filteredTrainedModels;
                } else {
                    this.trainedModels = this.filteredTrainedModels.filter(model => model.Clustering_type === this.filterType);
                }
            }
            this.coreModlesList = this.trainedModels;
            this.rawEvaluatedData = undefined;
            this.evaluatedResponse = undefined;
            this.modelSelectedForEvaluate = false;
            this.textForEvaluation = '';
            this.disabledRetrain = true;
        }, error => {
            this.trainedModels = undefined;
            this._appUtilsService.loadingEnded();
            this.ns.error('Something went wrong.');
        });
        if (this.isWordCloud != true)
            this.fetchAllUseCases();

        return false;
    }

    selectClusteringType(clusteringType) {
        this.resetClusteringEvalData();
        if (clusteringType === 'Text') {
            this.problemType['Clustering']['Text'] = 'True';
            this.problemType['Clustering']['Non-Text'] = 'False';
        } else {
            this.problemType['Clustering']['Text'] = 'False';
            this.problemType['Clustering']['Non-Text'] = 'True';
        }
    }

    modelTypeChange(modelType, correlationId) {
        this.clearClusterTextBoxes();
        this.clusterMapping = { 'ClusterInputs': { 'KMeans': {}, 'DBSCAN': {} } };
        const newObj = this.clusteredModels.find(newModel => newModel.CorrelationId === correlationId
            && newModel.ModelType === modelType);
        let modelStatus = 'Error';
        this.trainedModels.forEach((element, i) => {
            if (element.CorrelationId === newObj.CorrelationId) {
                element.ModelName = newObj.ModelName;
                element.PredictionURL = newObj.PredictionURL;
                element.ModifiedOn = newObj.ModifiedOn;
                element.ModelStatus = newObj.RequestStatus;
                element.StatusMessage = newObj.Message;
                element.selectedModelType = modelType;
                element.Clustering_type = newObj.Clustering_type;
                element.pageInfo = newObj.pageInfo;
                element.ingestData = newObj.ingestData;
                element.UniId = newObj.UniId;
                modelStatus = newObj.RequestStatus;
                element.selectedModelInList = modelType;
                element.Clusters = newObj.Clusters;
                element.DataSource = '';
                this.selectedClusterValue = newObj.Clusters;
                //  element.Clusters = 3;
                if (!this.coreUtilService.isNil(newObj.Suggestion)) {
                    element.Suggestion = newObj.Suggestion;
                }
                if (!this.coreUtilService.isNil(newObj.ingestData)) {
                    if (!this.coreUtilService.isNil(newObj.ingestData.Columnsselectedbyuser)) {
                        element.Columnsselectedbyuser = newObj.ingestData.Columnsselectedbyuser;
                        this.selectedIngestedColumnNames = newObj.ingestData.Columnsselectedbyuser;
                    }
                    if (!this.coreUtilService.isNil(newObj.ingestData.DataSource)) {
                        element.DataSource = newObj.ingestData.DataSource;
                    }
                }
            }
        });
        if (modelType === 'KMeans') {
            this.selectedModelsForEval.KMeans.Train_model = 'True';
            this.selectedModelsForEval.DBSCAN.Train_model = 'False';
            this.selectedModelsForEval.Agglomerative.Train_model = 'False';
        } else if (modelType === 'DBSCAN') {
            this.selectedModelsForEval.KMeans.Train_model = 'False';
            this.selectedModelsForEval.DBSCAN.Train_model = 'True';
            this.selectedModelsForEval.Agglomerative.Train_model = 'False';
        } else {
            this.selectedModelsForEval.KMeans.Train_model = 'False';
            this.selectedModelsForEval.DBSCAN.Train_model = 'False';
            this.selectedModelsForEval.Agglomerative.Train_model = 'True';
        }
        if (modelStatus === 'Completed' || modelStatus === 'Ready' || modelStatus === 'Task Complete') {
            this.disabledEvaluate = false;
            this.disabledRetrain = false;
        } else if (modelStatus === 'InProgress' || modelStatus === 'In - Progress') {
            this.disabledEvaluate = true;
            this.disabledRetrain = true;
        } else if (modelStatus === 'Error') {
            this.disabledEvaluate = true;
            this.disabledRetrain = false;
        } else if (modelStatus === 'ReTrain Failed') {
            this.disabledEvaluate = false;
            this.disabledRetrain = false;
        }
    }

    evaluateForClustering() {
        const textColumnsForEval = this.elem.nativeElement.querySelectorAll('.text-columns');
        const nonTextColumnsForEval = this.elem.nativeElement.querySelectorAll('.non-text-columns');
        const categoryColumnsForEval = this.elem.nativeElement.querySelectorAll('.category-columns');
        const objForEval = {};
        const dataForEval = [];
        let textEmpty = false;
        let nonTextEmpty = false;
        let categoryEmpty = false;
        nonTextColumnsForEval.forEach(nonTextEl => {
            const keyValue = nonTextEl.dataset.nontextval;
            objForEval[keyValue] = nonTextEl.valueAsNumber;
            if (this.coreUtilService.isNil(nonTextEl.valueAsNumber) || isNaN(nonTextEl.valueAsNumber)) {
                nonTextEmpty = true;
            }
        });
        categoryColumnsForEval.forEach(categoryEl => {
            const keyValue = categoryEl.dataset.categoryval;
            objForEval[keyValue] = categoryEl.value.trim();
            if (this.coreUtilService.isNil(categoryEl.value.trim())) {
                categoryEmpty = true;
            }
        });
        textColumnsForEval.forEach(textEl => {
            const keyValue = textEl.dataset.textval;
            objForEval[keyValue] = textEl.value.trim();
            if (this.coreUtilService.isNil(textEl.value.trim())) {
                textEmpty = true;
            }
        });
        if (textEmpty === false && nonTextEmpty === false && categoryEmpty === false) {
            dataForEval.push(objForEval);
            const bodyParams = {};
            let mapping = {};
            let isValidMapping = true;
            if (Object.keys(this.clusterMapping.ClusterInputs.KMeans).length > 0) {
                if (Object.keys(this.clusterMapping.ClusterInputs.KMeans).length > 0 && Object.keys(this.clusterMapping.ClusterInputs.KMeans).length < this.selectedClusterValue) {
                    isValidMapping = false;
                } else {
                    isValidMapping = true;
                    mapping = this.clusterMapping.ClusterInputs.KMeans;
                }
            } else {
                if (Object.keys(this.clusterMapping.ClusterInputs.DBSCAN).length > 0 && Object.keys(this.clusterMapping.ClusterInputs.DBSCAN).length < this.selectedClusterValue) {
                    isValidMapping = false;
                } else {
                    isValidMapping = true;
                    mapping = this.clusterMapping.ClusterInputs.DBSCAN;
                }
            }
            if (isValidMapping === true) {
                this._appUtilsService.loadingStarted();
                const payload = {
                    'CorrelationId': this.selectedModelCorrelationId,
                    'PageInfo': 'Evaluate_API',
                    'UserId': this._appUtilsService.getCookies().UserId,
                    'DCUID': this.paramData.deliveryConstructUId,
                    'ClientID': this.paramData.clientUID,
                    'UniId': '',
                    'CreatedDate': new Date(),
                    'Token': '',
                    'Data': dataForEval,
                    'SelectedModels': this.selectedModelsForEval,
                    'bulk': false,
                    'mapping': mapping
                };
                this.problemStatementService.ClusteringEvaluate(payload, bodyParams).subscribe(response => {
                    this._appUtilsService.loadingEnded();
                    if (response.hasOwnProperty('message')) {
                        if (response.message === '') {
                            this.disabledTrain = true;
                            this.disabledRetrain = true;
                            this.uploadedData = [];
                            this.rawEvaluatedData = response;
                            if (response.hasOwnProperty('Predictions')) {
                                this.evaluatedResponse = response.Predictions;
                            } else {
                                this.evaluatedResponse = '';
                            }
                        } else {
                            this.ns.error(response.message);
                        }
                    } else {
                        this.disabledTrain = true;
                        this.disabledRetrain = true;
                        this.uploadedData = [];
                        this.rawEvaluatedData = response;
                        if (response.hasOwnProperty('Predictions')) {
                            this.evaluatedResponse = response.Predictions;
                        } else {
                            this.evaluatedResponse = '';
                        }
                    }
                }, error => {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Something went wrong.');
                });
            } else {
                this.ns.error('Kindly map all the cluster inputs.');
            }
        } else {
            this.ns.error('Kindly enter the value for all text boxes and enter only numbers for number type text boxes.');
        }
    }

    /* onPIConfirmation(optionValue) {
      this.PIConfirmation = optionValue;
    } */

    resetClusteringEvalData() {
        /* const textColumnsForEval = this.elem.nativeElement.querySelectorAll('.text-columns');
        const nonTextColumnsForEval = this.elem.nativeElement.querySelectorAll('.non-text-columns');
        const categoryColumnsForEval = this.elem.nativeElement.querySelectorAll('.category-columns');
        nonTextColumnsForEval.forEach(nonTextEl => {
          nonTextEl.value = null;
        });
        categoryColumnsForEval.forEach(categoryEl => {
          categoryEl.value = null;
        });
        textColumnsForEval.forEach(textEl => {
          textEl.value = null;
        }); */
        this.evaluatedResponse = undefined;
        this.clearClusterTextBoxes();
        this.textColumns = [];
        this.nonTextColumns = [];
        this.categoryColumns = [];
    }

    clearClusterTextBoxes() {
        const textColumnsForEval = this.elem.nativeElement.querySelectorAll('.text-columns');
        const nonTextColumnsForEval = this.elem.nativeElement.querySelectorAll('.non-text-columns');
        const categoryColumnsForEval = this.elem.nativeElement.querySelectorAll('.category-columns');
        nonTextColumnsForEval.forEach(nonTextEl => {
            nonTextEl.value = null;
        });
        categoryColumnsForEval.forEach(categoryEl => {
            categoryEl.value = null;
        });
        textColumnsForEval.forEach(textEl => {
            textEl.value = null;
        });
    }

    applyFilters(clusteringType) {
        this.resetClusteringEvalData();
        this.filterType = clusteringType;
        if (this.filteredTrainedModels.length === 0) {
            this.trainedModels = undefined;
        } else {
            if (this.filterType === 'All') {
                this.trainedModels = this.filteredTrainedModels;
            } else {
                this.trainedModels = this.filteredTrainedModels.filter(model => model.Clustering_type === this.filterType);
            }
        }
    }

    changeDetectionForTrainModel(problemType, selectedOptions) {
        let isProblemTypeChanged = false;
        let isSelectedOptionsChanged = false;
        let isSelectedStopWords = false;
        let isSelectedNgramValue = false;
        if (JSON.stringify(this.oldProblemType) !== JSON.stringify(this.problemType)) {
            isProblemTypeChanged = true;
        }
        if (JSON.stringify(this.oldSelectedOptions) !== JSON.stringify(this.selectedOptions)) {
            isSelectedOptionsChanged = true;
        }
        if (JSON.stringify(this.oldNgramSelection) !== JSON.stringify(this.selectedNgramVal)) {
            isSelectedNgramValue = true;
        }
        if (JSON.stringify(this.oldSeletedStopWords) !== JSON.stringify(this.clusteringSW)) {
            isSelectedStopWords = true;
        }

        if (isProblemTypeChanged === true || isSelectedOptionsChanged === true || isSelectedNgramValue === true || isSelectedStopWords === true) {
            return true;
        } else {
            return false;
        }
    }

    replaceFile() {
        this.uploadedData = [];
        //  this.fileInput.nativeElement.value = '';
    }

    /* UseCase Tab functionality :start */
    loadUseCases() {
        this.enableModelTab = false;
        this.enablePublishUseCaseTab = true;
        this.disabledTrainSim = true;
        this.disabledReTrainSim = true;
        this.disableUseCaseTrain = true;
        this.fetchAllUseCases();
    }

    loadModel() {
        this.enableModelTab = true;
        this.enablePublishUseCaseTab = false;
        this.disabledTrainSim = true;
        this.disabledReTrainSim = true;
        this.usecaseSampleInputshow = false;
        if (this.isClustering === true || this.isWordCloud === true) {
            this.getCusteringModels();
        } else {
            this.getAICoreModels();
        }
    }

    fetchAllUseCases() {
        this._appUtilsService.loadingStarted();
        this.publishUseCases = undefined;
        this.disableUseCaseTrain = true;
        // this.selectedServiceId = '28179a56-38b2-4d69-927d-0a6ba75da377';
        this.problemStatementService.fetchAICoreUseCases(this.selectedServiceId).subscribe(response => {
            if (response) {
                this._appUtilsService.loadingEnded();
                if (response.length === 0) {
                    this.publishUseCases = undefined;
                } else {
                    this.publishUseCases = response;
                }
                this.corePublishModelList = this.publishUseCases;
            } else {
                this._appUtilsService.loadingEnded();
            }
        }, error => {
            this._appUtilsService.loadingEnded();
            this.publishUseCases = undefined;
            this.ns.error('Something went wrong');
        });
    }


    selectUseCase(usecase, index, isDevPrediction) {
        this.selectedusecaseSampleInputBtnIndex = index;
        if (isDevPrediction) {
            if (this.coreUtilService.isNil(usecase.SourceDetails)) {
                this.disableUseCaseTrain = true;
                this.ns.warning(`Usecase Training can't be initiated, since Data Source Details are not available.`);
            } else {
                this.disableUseCaseTrain = false;
            }
        } else {
            this.disableUseCaseTrain = false;
        }
    }


    openUsecaseSampleInputWindow(index: number, usecaseDtl) {
        this.selectedusecaseSampleInputIndex = index;
        this.useCaseAttributes = usecaseDtl.InputColumns;
        this.useCaseStopWords = usecaseDtl.StopWords;

        if (this.isSampleInputPopupClosed) {
        } else {
            this.usecaseSampleInputshow = true;
        }
        this.isSampleInputPopupClosed = false;
    }

    closeUsecaseSampleInputWindow(index: number) {
        this.selectedusecaseSampleInputIndex = index;
        this.usecaseSampleInputshow = false;
        return this.usecaseSampleInputshow;
    }

    trainSelectedUseCase(usecaseDetails, isDevPrediction) {
        if (isDevPrediction) {
            const openTemplateAfterClosed =
                this.dialogService.open(TemplateNameModalComponent,
                    { data: { title: 'Enter Model Name', pageInfo: 'CreateAICoreModel' } }).afterClosed.pipe(
                        tap(modalName => {
                            if (modalName !== undefined) {
                                this.trainUseCaseAPICall(usecaseDetails, modalName);
                            }
                        })
                    );
            openTemplateAfterClosed.subscribe();
        } else {
            const useCaseDtl = {
                Source: '',
                pad: '',
                CorrelationId: ''
            };
            this.ifUsecaseModelSelected = usecaseDetails;
            if (usecaseDetails.SourceName != null) {
                useCaseDtl['Source'] = usecaseDetails.SourceName;
                if (this.env == 'PAM' || this.env == 'FDS') {
                    useCaseDtl['custom'] = JSON.parse(usecaseDetails.SourceDetails).Customdetails;
                } else {
                    useCaseDtl['pad'] = JSON.parse(usecaseDetails.SourceDetails).pad;
                }
            } else if (usecaseDetails.SourceName == null) { // for custom connector SourceName will be null
                useCaseDtl['Source'] = 'File';
                useCaseDtl['pad'] = '';
            } else if (usecaseDetails.SourceName === 'DataSet') {
                useCaseDtl['Source'] = 'DataSet';
                useCaseDtl['pad'] = '';
            }

            useCaseDtl['CorrelationId'] = usecaseDetails.CorrelationId;
            this.selectedModelData = useCaseDtl;

            let selectedUseCaseM = _.filter(this.publishUseCases, function (item) {
                return item.CorrelationId == usecaseDetails.CorrelationId;
            });
            let dataSetUniqueId
            if (selectedUseCaseM.length !== 0) {
                if (selectedUseCaseM[0].DataSetUID !== "null") {
                    dataSetUniqueId = selectedUseCaseM[0].DataSetUID
                } else {
                    dataSetUniqueId = null;
                }
            } else {
                dataSetUniqueId = null;
            }
            // console.log('test-----------', selectedUseCaseM[0].DataSetUId);

            const apiUploadAfterClosed = this.dialogService.open(UploadSimilarityAnalysisAPIComponent,
                {
                    data: {
                        retrainModel: true,
                        selectedModelData: this.selectedModelData,
                        entityData: undefined,
                        serviceId: this.selectedServiceId,
                        actionScreen: 'UseCase',
                        clientUID: this.payloadForGetAICoreModels.clientid,
                        deliveryConstructUID: this.payloadForGetAICoreModels.dcid,
                        userId: this.payloadForGetAICoreModels.userid,
                        dataSetUId: dataSetUniqueId // 'fd7cd16e-948f-49a9-8780-a3b837af379f' 
                    }
                }).afterClosed
                .pipe(
                    tap(filesData => filesData ? this.openModalNameDialog(filesData, false) : '')
                );

            apiUploadAfterClosed.subscribe();
        }
    }

    trainUseCaseAPICall(usecaseDetails, modelName) {
        const param = {
            'clientId': this.paramData.clientUID,
            'deliveryConstructId': this.paramData.deliveryConstructUId,
            'serviceId': usecaseDetails.ServiceId,
            'applicationId': usecaseDetails.ApplicationId,
            'usecaseId': usecaseDetails.UsecaseId,
            'modelName': modelName,
            'userId': this._appUtilsService.getCookies().UserId,
            'isManual': 'true'
        };
        this._appUtilsService.loadingStarted();
        this.problemStatementService.trainModelDeveloperPrediction(param).subscribe(data => {
            this._appUtilsService.loadingEnded();
            this.disableUseCaseTrain = true;

            if (data.hasOwnProperty('IsSuccess')) {
                if (data['IsSuccess'] === true) {
                    if (data.ReturnValue.ResponseData.message === 'Success') {
                        this.ns.success('Model Training initiated successfully');
                    }
                } else {
                    this.disableUseCaseTrain = true;
                    if (data.hasOwnProperty('Message')) {
                        if (data['Message'] !== '') {
                            this.ns.error(data['Message']);
                        } else {
                            this.ns.error('Python error in training');
                        }
                    }
                }
            }
            // this.usecase_name = null;
        }, error => {
            this.disableUseCaseTrain = true;
            this.usecase_name = null;
            this._appUtilsService.loadingEnded();
            this.ns.error('something went wrong in training usecase');
        });
    }

    saveUsecase(model, rowIndex) {
        // clearimg the custom config local Storage
        this._deploymodelService.clearCustomConfigStorage();
        //
        // if (rowIndex)
        this.isPublishusecase = false;
        // }
        const openTemplateAfterClosed =
            this.dialogService.open(TemplateNameModalComponent, {
                data: {
                    title: 'Enter UseCase Name', correlationid: model.CorrelationId,
                    clientId: this.paramData.clientUID, deliveryConstructId: this.paramData.deliveryConstructUId,
                    isWordCloud: this.isWordCloud, DataSource: model.DataSource
                }
            }).afterClosed.pipe(
                tap(result => result ? this.publishSelectedUsecase(model, result) : '')
            );
        openTemplateAfterClosed.subscribe();
        return false;
    }

    publishSelectedUsecase(model, resusltSet) {

        let selectedUseCaseM = _.filter(this.trainedModels, function (item) {
            return item.CorrelationId == model.CorrelationId;
        });
        let dataSetUniqueId
        if (selectedUseCaseM.length !== 0) {
            dataSetUniqueId = selectedUseCaseM[0].DataSetUId
        } else {
            dataSetUniqueId = null;
        }
        const requestPayload = {
            'UsecaseName': resusltSet.UsecaseName,
            'CorrelationId': model.CorrelationId,
            'ModelName': model.ModelName,
            'Description': resusltSet.Description,
            'ApplicationName': resusltSet.ApplicationName,
            'ApplicationId': resusltSet.ApplicationId,
            'ServiceId': this.selectedServiceId,
            'DataSetUId': dataSetUniqueId
        };
        requestPayload['IsCarryOutRetraining'] = resusltSet.IsCarryOutRetraining;
        requestPayload['IsOnline'] = resusltSet.IsOnline;
        requestPayload['IsOffline'] = resusltSet.IsOffline;
        requestPayload['Retraining'] = resusltSet.Retraining;
        requestPayload['Training'] = resusltSet.Training;
        requestPayload['Prediction'] = resusltSet.Prediction;
        this._appUtilsService.loadingStarted();
        this.problemStatementService.publishUseCase(requestPayload).subscribe(resData => {
            this._appUtilsService.loadingEnded();
            if (resData) {
                if (resusltSet.IsOnline == true || resusltSet.IsOffline == true) {
                    const trainingCustomConfig = sessionStorage.getItem(`${ServiceTypes.Training}${this.ConfigStorageKey}`);
                    const predictionCustomConfig = sessionStorage.getItem(`${ServiceTypes.Prediction}${this.ConfigStorageKey}`);
                    const reTrainingCustomConfig = sessionStorage.getItem(`${ServiceTypes.ReTraining}${this.ConfigStorageKey}`);
                    if (trainingCustomConfig != null || predictionCustomConfig != null || reTrainingCustomConfig != null) {
                        this.saveCustomConfiguration(resData);
                    }
                }
                this.ns.success('Usecase published successfully');
            } else {
                this.ns.error(resData);
            }
        }, error => {
            this._appUtilsService.loadingEnded();
            let errorMsg = '';
            if (error.error === 'Model Name already exist') {
                errorMsg = (error.error).replace('Model', 'Usecase');
                this.ns.error(errorMsg);
            } else {
                // this.ns.error('something went wrong in publishusecase');
                this.ns.error(error.error);
            }
        });
    }

    showUsecasePopOver(rowIndex, status?) {
        // this.isUsecasePopOverOpen = false; // close already opened popup
        // this.selectedModelForPopoverIndex = rowIndex;
        if (status !== undefined) {
            this.ns.warning('Training is not completed for this Model');
            return 0;
        }
        // this.isUsecasePopOverOpen = !this.isUsecasePopOverOpen;
    }

    trainAIModelsFromUseCase(usecaseDetails, modelName, fileData) {
        if (usecaseDetails.SourceName == null || usecaseDetails.SourceName === 'Custom') { // for custom connector SourceName will be null
            usecaseDetails.SourceName = 'File';
        }
        const param = {
            'clientId': this.paramData.clientUID,
            'deliveryConstructId': this.paramData.deliveryConstructUId,
            'serviceId': usecaseDetails.ServiceId,
            'applicationId': usecaseDetails.ApplicationId,
            'usecaseId': usecaseDetails.UsecaseId,
            'modelName': modelName,
            'userId': this._appUtilsService.getCookies().UserId,
            'DataSource': fileData[0].source,
            'DataSourceDetails': '',
            'Language': this.language
        };
        this._appUtilsService.loadingStarted();
        this.problemStatementService.trainAIModelsFromUseCase(param, fileData).subscribe(data => {
            this._appUtilsService.loadingEnded();
            this.disableUseCaseTrain = true;
            this.ifUsecaseModelSelected = undefined;

            if (data.constructor === Object) {
                if (data.ModelStatus === 'Error') {
                    this.ns.error(data.StatusMessage);
                } else {
                    this.ns.success('Model Training initiated successfully');
                }
            } else if (data.constructor === String) {
                this.ns.error(data);
            }
            // this.usecase_name = null;
        }, error => {
            this.ifUsecaseModelSelected = undefined;
            this.disableUseCaseTrain = true;
            this.usecase_name = null;
            this._appUtilsService.loadingEnded();
            this.ns.error(error.error);

        });
    }

    /* UseCase Tab functionality :end */

    toArray(n: number): number[] {
        return Array(n);
    }

    onClickSuggestion(suggestion, index, selectedModel, model) {
        this.selectedValue = this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index];
        if (selectedModel === 'KMeans') {
            this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index] = suggestion;
            this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.KMeans;
            this.currentSuggestion = this.clusterMapping.ClusterInputs.KMeans;
        } else {
            this.clusterMapping.ClusterInputs.DBSCAN['Cluster ' + index] = suggestion;
            this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.DBSCAN;
            this.currentSuggestion = this.clusterMapping.ClusterInputs.DBSCAN;
        }
        this.selectedNewValue = suggestion;
    }

    suggestionInputChange(event, index, selectedModel, model) {
        if (selectedModel === 'KMeans') {
            if (event.target.value === '') {
                delete this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index];
                this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.KMeans;
                this.currentSuggestion = this.clusterMapping.ClusterInputs.KMeans;
            } else {
                this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index] = event.target.value;
                this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.KMeans;
                this.currentSuggestion = this.clusterMapping.ClusterInputs.KMeans;
            }
        } else {
            if (event.target.value === '') {
                delete this.clusterMapping.ClusterInputs.DBSCAN['Cluster ' + index];
                this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.DBSCAN;
                this.currentSuggestion = this.clusterMapping.ClusterInputs.DBSCAN;
            } else {
                this.clusterMapping.ClusterInputs.DBSCAN['Cluster ' + index] = event.target.value;
                this.selectionChangeClusterMapping[model.CorrelationId] = this.clusterMapping.ClusterInputs.DBSCAN;
                this.currentSuggestion = this.clusterMapping.ClusterInputs.DBSCAN;
            }
        }
    }

    arrayToObject(array) {
        const obj = {};
        for (let index = 0; index < array.length; index++) {
            obj[index] = '';
        }
        return obj;
    }

    clusteringViewData(correlationId, modelType, action?) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.clusteringViewData(correlationId, modelType).subscribe(data => {
            this._appUtilsService.loadingEnded();
            if (data.length > 0) {
                if (action === 'Popup') {
                    const tableData = data;
                    const showDataColumnsList = Object.keys(data[0]);
                    this.dialogService.open(ShowDataComponent, {
                        data: {
                            'tableData': tableData,
                            'columnList': showDataColumnsList,
                            'problemTypeFlag': false
                        }
                    });
                } else if (action === 'Download') {
                    const self = this;
                    if (this.instanceType === 'PAM' || this.env === 'FDS') {
                        this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                            if (confirmationflag === true) {
                                const dataForDownload = data || [];
                                self._excelService.exportAsExcelFile(dataForDownload, 'DownloadedData');
                                self.ns.success('Downloaded Data Successfully');
                            }
                        });
                    } else {
                        userNotification.showUserNotificationModalPopup();
                        $(".notification-button-close").click(function () {
                            const dataForDownload = data || [];
                            self._excelService.exportAsPasswordProtectedExcelFile(dataForDownload, 'DownloadedData').subscribe(response => {
                                self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                                let binaryData = [];
                                binaryData.push(response);
                                let downloadLink = document.createElement('a');
                                downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                                downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
                                document.body.appendChild(downloadLink);
                                downloadLink.click();
                            }, (error) => {
                                self.ns.error(error);
                            });
                            self.ns.success('Downloaded Data Successfully');
                        });
                    }
                }
            } else {
                this.ns.error('No Data Found');
            }
        },
            error => {
                this._appUtilsService.loadingEnded();
                this.ns.error('Something went wrong to get View Upload Data.');
            });
    }

    clusteringDownloadMappedData(correlationId, modelType, uniId) {
        this._appUtilsService.loadingStarted();
        let clusterMappingForDownload;
        if (modelType === 'KMeans') {
            clusterMappingForDownload = this.clusterMapping.ClusterInputs.KMeans;
        } else {
            clusterMappingForDownload = this.clusterMapping.ClusterInputs.DBSCAN;
        }
        const params = {
            'CorrelationId': correlationId,
            'pageInfo': 'MapClusteringData',
            'UserId': this._appUtilsService.getCookies().UserId,
            'UniId': uniId,
            'modeltype': modelType,
            'MappingData': clusterMappingForDownload
        };
        this.problemStatementService.clusteringDownloadMappedData(params).subscribe(data => {
            this._appUtilsService.loadingEnded();
            if (data.hasOwnProperty('Message')) {
                if (data.Message === 'Success') {
                    this.monitorDownloadMappedDataStatus(correlationId, modelType);
                } else {
                    this.ns.error(data.Message);
                }
            } else {
                this.ns.error('Something went wrong');
            }
        },
            error => {
                this._appUtilsService.loadingEnded();
                this.ns.error('Something went wrong');
            });
    }

    monitorDownloadMappedDataStatus(correlationId, modelType) {
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    self._appUtilsService.loadingStarted();
                    self.ingestStatusSubscription = self.problemStatementService.downloadMappedDataStatus(correlationId, modelType, 'MapClusteringData')
                        .subscribe(
                            data => {
                                if (data['Status'] === 'P' || data['Status'] === null) {
                                    self.retryDownloadStatus(correlationId, modelType);
                                } else if (data['Status'] === 'C') {
                                    self._appUtilsService.loadingImmediateEnded();
                                    const dataForDownload = data['InputData'] || [];
                                    self._excelService.exportAsExcelFile(dataForDownload, 'DownloadedData');
                                } else if (data['Status'] === 'E') {
                                    self._appUtilsService.loadingImmediateEnded();
                                    self.ns.error(data['Message']);
                                } else {
                                    self.ns.error(`Error occurred: Due to some backend data process
          the relevant data could not be produced. Please try again while we troubleshoot the error`);
                                    self.unsubscribe();
                                    self._appUtilsService.loadingImmediateEnded();
                                }
                            }, error => {
                                self._appUtilsService.loadingImmediateEnded();
                                self.ns.error('Something went wrong.');
                            });
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                // this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                //  if (confirmationflag === true) {
                self._appUtilsService.loadingStarted();
                self.ingestStatusSubscription = self.problemStatementService.downloadMappedDataStatus(correlationId, modelType, 'MapClusteringData')
                    .subscribe(
                        data => {
                            if (data['Status'] === 'P' || data['Status'] === null) {
                                self.retryDownloadStatus(correlationId, modelType);
                            } else if (data['Status'] === 'C') {
                                self._appUtilsService.loadingImmediateEnded();
                                const dataForDownload = data['InputData'] || [];
                                self._excelService.exportAsPasswordProtectedExcelFile(dataForDownload, 'DownloadedData').subscribe(response => {
                                    self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                                    let binaryData = [];
                                    binaryData.push(response);
                                    let downloadLink = document.createElement('a');
                                    downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                                    downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
                                    document.body.appendChild(downloadLink);
                                    downloadLink.click();
                                }, (error) => {
                                    self.ns.error(error);
                                });
                                self.ns.success('Downloaded Data Successfully');
                            } else if (data['Status'] === 'E') {
                                self._appUtilsService.loadingImmediateEnded();
                                self.ns.error(data['Message']);
                            } else {
                                self.ns.error(`Error occurred: Due to some backend data process
          the relevant data could not be produced. Please try again while we troubleshoot the error`);
                                self.unsubscribe();
                                self._appUtilsService.loadingImmediateEnded();
                            }
                        }, error => {
                            self._appUtilsService.loadingImmediateEnded();
                            self.ns.error('Something went wrong.');
                        });
                // }
            });
        }
    }

    retryDownloadStatus(correlationId, modelType) {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.monitorDownloadMappedDataStatus(correlationId, modelType));
        return this.timerSubscription;
    }

    mapClusterData(correlationId, model?: any) {
        this.clusterChartTextView = false;
        const columnsForEval = this.elem.nativeElement.querySelectorAll('.clusterMapping' + correlationId);
        const objForEval = {};
        const dataForEval = [];
        let inputEmpty = false;
        columnsForEval.forEach(inputEl => {
            const keyValue = inputEl.dataset.mappingColumn;
            objForEval[keyValue] = inputEl.value.trim();
            if (this.coreUtilService.isNil(inputEl.value.trim())) {
                inputEmpty = true;
            }
        });
        if (inputEmpty === false) {
            const body = {
                'CorrelationId': this.selectedModelCorrelationId,
                'DCUID': this.paramData.deliveryConstructUId,
                'ClientID': this.paramData.clientUID,
                'mapping': this.clusterMapping.ClusterInputs[model.selectedModelInList]
            }
            this.problemStatementService.saveClusterMapData(body).subscribe(data => {
                // this.getCusteringModels();
                this._appUtilsService.loadingStarted();
                const params = {
                    'clientid': this.paramData.clientUID,
                    'dcid': this.paramData.deliveryConstructUId,
                    'serviceid': this.selectedServiceId,
                    'userid': this._appUtilsService.getCookies().UserId
                };
                this.problemStatementService.getCusteringModels(params).subscribe(models => {
                    this.backUpClusteringModels = models;
                    this.pageloadCluster = _.cloneDeep(models);
                    this._appUtilsService.loadingEnded();
                    this.disableDwnldMappedData = false;
                    this.ns.success('Your data has been mapped successfully.');
                });
            }, (error) => {
                this._appUtilsService.loadingEnded();
                this.ns.error('Something went wrong.');
            });
        } else {
            this.ns.error('Kindly map all the cluster inputs.');
        }
    }

    autoFillClusterData(numberOfClusters, suggestions, modelType, model) {
        this.clusterMapping = { 'ClusterInputs': { 'KMeans': {}, 'DBSCAN': {} } };
        if (modelType === 'KMeans') {
            if (Object.keys(model?.mapping).length && !this.selectionChangeClusterMapping[model.CorrelationId]) {
                this.clusterMapping.ClusterInputs.KMeans = model.mapping;
                this.disableDwnldMappedData = false;
            } else if (this.selectionChangeClusterMapping[model.CorrelationId]) {
                this.clusterMapping.ClusterInputs.KMeans = this.selectionChangeClusterMapping[model.CorrelationId];
            } else {
                for (let index = 0; index < numberOfClusters; index++) {
                    this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index] = suggestions[index]?.[0];
                }
            }
        } else {
            if (Object.keys(model?.mapping).length && !this.selectionChangeClusterMapping[model.CorrelationId]) {
                this.clusterMapping.ClusterInputs.DBSCAN = model.mapping;
                this.disableDwnldMappedData = false;
            } else if (this.selectionChangeClusterMapping[model.CorrelationId]) {
                this.clusterMapping.ClusterInputs.DBSCAN = this.selectionChangeClusterMapping[model.CorrelationId];
            } else {
                for (let index = 0; index < Object.keys(suggestions).length; index++) {
                    let suggestedKeyIndex = Object.keys(suggestions);
                    this.suggestedKeyIndexVal = suggestedKeyIndex;
                    if (suggestedKeyIndex[index] != undefined) {
                        if (suggestedKeyIndex[index] != undefined && suggestedKeyIndex[index].indexOf("-1") > -1) {
                            this.clusterMapping.ClusterInputs.DBSCAN['Cluster -1'] = 'Noise Point';
                        } else {
                            this.clusterMapping.ClusterInputs.DBSCAN['Cluster ' + index] = suggestions[index][0];
                        }
                    }
                }
            }
        }
    }

    onEvaluationColSelected(value) {
        this.columnSelectValForEvaluate = value;
    }

    refreshSimilarityAnalyticsModels() {
        this.disabledTrainSim = true;
        this.disabledReTrainSim = true;
        this.disableUseCaseTrain = true;
        this.getAICoreModels();
        this.fetchAllUseCases();
        return false;
    }


    isSpecialCharacter(input: string) {
        const regex = /^[A-Za-z0-9_.,'"\- ]+$/;
        if (input && input.length > 0) {
            const isValid = regex.test(input);
            if (!isValid) {
                // this.ns.warning('No special characters allowed.');
                return 0; // Return 0 , if input string contains special character
            } else {
                return 1; // Return 1 , if input string does not contains special character
            }
        }
    }

    deleteModel(model) {
        this._appUtilsService.loadingStarted();
        this.isDelete = true;
        this.problemStatementService.deleteAIServiceModel(model.CorrelationId).subscribe(response => {
            if (response) {
                this._appUtilsService.loadingEnded();
                this.isDelete = false;
                if (model.ModelStatus === 'Completed' || model.ModelStatus === 'Ready' || model.ModelStatus === 'Task Complete') {
                    this.ns.success(response);
                    if (this.isClustering == true) {
                        this.getCusteringModels()
                    } else {
                        this.getAICoreModels();
                    }
                } else if (model.ModelStatus === 'InProgress' || model.ModelStatus === 'In - Progress'
                    || model.ModelStatus === 'Upload InProgress' || model.ModelStatus === 'Training is not initiated') {
                    if (response === 'Model deleted successfully') {
                        this.ns.success(response);
                        if (this.isClustering == true) {
                            this.getCusteringModels()
                        } else {
                            this.getAICoreModels();
                        }
                    } else {
                        this.ns.warning(response);
                    }
                } else if (model.ModelStatus === 'Error' || model.ModelStatus === 'Warning') {
                    this.ns.success(response);
                    if (this.isClustering == true) {
                        this.getCusteringModels()
                    } else {
                        this.getAICoreModels();
                    }
                } else if (model.ModelStatus === 'ReTrain Failed') {
                    this.ns.success(response);
                    if (this.isClustering == true) {
                        this.getCusteringModels()
                    } else {
                        this.getAICoreModels();
                    }
                }
            } else {
                this._appUtilsService.loadingEnded();
                this.isDelete = false;
                this.ns.error(response);
            }
        }, error => {
            this._appUtilsService.loadingEnded();
            this.isDelete = false;
            this.ns.error(error.error);
        });
        return false;
    }

    deleteUseCaseModel(useCase) {
        this.problemStatementService.deleteAIServicePublishUseCase(useCase.UsecaseId).subscribe(response => {
            if (response) {
                this.ns.success(response);
                this.fetchAllUseCases();
            } else {
                this.ns.error(response);
            }
        }, error => {
            this.ns.error(error.error);
        });
    }

    refreshPredicationData() {
        if (this.enableModelTab) {
            this.getAICoreModels();
        } else if (this.enablePublishUseCaseTab) {
            this.fetchAllUseCases();
        }
    }

    toggleNavBar() {
        this.isnavBarToggle = !this.isnavBarToggle;
        return false;
    }

    toggleNavBarLabels() {
        this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
        return false;
    }

    selectColumnNames(event, model, columnName) {
        this.value07 = [];
        this.value07 = model.ingestData.Columnsselectedbyuser;
        let selectIndex: number = 0;
        this.trainedModels.forEach(model => {
            if (model.CorrelationId == this.selectedModelCorrelationId) {
                if (event.target.checked) {
                    this.modelColumnValue = model.ingestData.Columnsselectedbyuser;
                    model.ingestData.Columnsselectedbyuser.push(columnName);
                    selectIndex = model.unSelectedColumnNames.indexOf(columnName, 0);
                    if (selectIndex > -1) {
                        model.unSelectedColumnNames.splice(selectIndex, 1);
                    }
                } else {
                    selectIndex = model.ingestData.Columnsselectedbyuser.indexOf(columnName, 0);
                    if (selectIndex > -1) {
                        model.ingestData.Columnsselectedbyuser.splice(selectIndex, 1);

                        model.unSelectedColumnNames.push(columnName)
                        this.disabledGenerateWC = false;
                    }
                }
            }
        });

        this.newDataCheck = Object.assign({}, model.ingestData.Columnsselectedbyuser);
        if (JSON.stringify(this.originalDataCheck) === JSON.stringify(this.newDataCheck)) {
            this.disabledGenerateWC = true;
        }
        else {
            this.disabledGenerateWC = false;
        }
    }

    selectAll() {
        this.toggleSelect = !this.toggleSelect;
        this.trainedModels.forEach(model => {
            if (model.CorrelationId == this.selectedModelCorrelationId) {
                model.ingestData.Columnsselectedbyuser = model.ColumnNames;
                this.disabledGenerateWC = false;
                model.ingestData.Columnsselectedbyuser = model.ingestData.Columnsselectedbyuser.filter((columnName, index, self) =>
                    index === self.findIndex((column) => (
                        column === columnName
                    ))
                )
                model.unSelectedColumnNames = [];
            }
        });
    }

    deselectAll() {
        this.toggleSelect = !this.toggleSelect;
        this.trainedModels.forEach(model => {
            if (model.CorrelationId == this.selectedModelCorrelationId) {
                model.ingestData.Columnsselectedbyuser = [];
                model.unSelectedColumnNames = model.ColumnNames;
                if (model.unSelectedColumnNames = model.unSelectedColumnNames.filter((columnName, index, self) =>
                    index === self.findIndex((column) => (
                        column === columnName
                    ))
                )) {
                    this.disabledGenerateWC = true;
                }
                else {
                    this.disabledGenerateWC = false;
                }
            }
        });
    }

    // Generate Word Cloud
    generateWC() {
        const body = {
            'CorrelationId': this.selectedModelCorrelationId,
            'StopWords': [],
            'SelectedColumns': []
        }
        this.isFlag = false;
        let filterlist = [];

        this._appUtilsService.loadingStarted();

        this.trainedModels.forEach(model => {
            if (model.CorrelationId == this.selectedModelCorrelationId) {
                if (model.StopWords.length > 0) {
                    filterlist = model.StopWords;
                }

                if (model.ingestData.StopWords !== null) {
                    if (model.ingestData.StopWords.length > 0) {
                        model.ingestData.StopWords.forEach(element => {
                            if (filterlist.includes(element) == false) {
                                filterlist.push(element);
                            }
                        });
                    }
                }

                body.StopWords = filterlist;

                if (model.ingestData.StopWords !== null) {
                    if (model.StopWords.length > 0 && model.ingestData.StopWords.length > 0) {
                        model.StopWords.forEach(element => {
                            if (model.ingestData.StopWords.includes(element)) {
                                model.ingestData.StopWords.splice(element);
                            }
                        });
                    }
                }

                body.SelectedColumns = model.ingestData.Columnsselectedbyuser;
            }
        });

        this.problemStatementService.getWordCloudGeneration(body)
            .subscribe(
                data => {
                    if (data && data['ReturnValue']['ResponseData']['message'] === 'Success') {
                        this.selectedWCImage = data['ReturnValue']['ResponseData']['output'];
                    } else {
                        this.ns.error(data['ReturnValue']['ResponseData']['message']);
                    }
                    this._appUtilsService.loadingEnded();
                },
                error => {
                    this._appUtilsService.loadingEnded();
                    this.ns.error('Failed to generate word cloud.');
                }
            );
    }

    setEntityView() {
        this.provideTrainingDataVolume = '';
        this.uploadedData = [];
        this.datasetname = undefined;
        if (this.isDevTest) {
            this.trainingDataVolume = true;
        }
        if (!this.entityLoader) {
            this.entityLoader = true;
        }

        this.fileView = false;
        this.entityView = true;
        this.datasetView = false;
        this.customDataView = false;
        this.entityFormGroup = this.formBuilder.group({
            startDateControl: [''],
            endDateControl: [''],
            entityControl: [''],
            deliveryTypeControl: ['']
        });

        // if (this.entityLoader) {
        //   this.entityList = [];
        //   if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        //     this.deliveryTypeList = ['AIops'];
        //   } else {
        //     this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others', 'PPM'];
        //   }
        //   if (this.reTrainModel === false) {
        //     this.getEntityDetails();
        //   } else if (this.reTrainModel === true) {
        //     this.entityLoader = false;
        //   }
        // }
        if (!((this.env == 'FDS' || this.env === 'PAM') && (this.requestType == 'AM' || this.requestType == 'IO'))) {
            if (this.entityLoader) {
                this.entityList = [];
                this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others', 'PPM'];
                if (this.reTrainModel == false) {
                    this.getEntityDetails();
                } else {
                    this.entityLoader = false;
                }
            }
        } else {
            this.deliveryTypeList = ['AIops'];
            this.entityLoader = false;
        }
        this.clearCustomData();
    }

    onChangeOFEntity() {
        const entityValue = this.entityFormGroup.get('entityControl').value;
        // this.entityselection = true;
        let entityValueLength = entityValue?.length;
        entityValueLength = 1;
        const sourceLength = entityValueLength;
        if (sourceLength < 2) {
            if (this.entityFormGroup.get('startDateControl').status === 'VALID' &&
                this.entityFormGroup.get('endDateControl').status === 'VALID') {
                let selectedEntityObj = { 'name': '', 'sub': '' };
                this.entityList.forEach(element => {
                    if (element.name == this.selectedEntity) {
                        selectedEntityObj = element;
                    }
                });
                this.entityArray[0] = selectedEntityObj;
                // let entityArrayLength = this.entityArray.length;
                // this.sourceTotalLength = this.uploadedData.length + entityArrayLength;
            }
        } else {
            this.ns.error('Already a file is uploaded.');
        }
    }

    onChangeOfDataset() {
        this.datasetEntered = true;
    }

    onChangeOfDeliveryType(deliveryName) {
        const deliveryTypeValue = this.entityFormGroup.get('deliveryTypeControl').value;
        //  this.entityList = [];
        // this.deliveryTypeSelection = true; Not in use
        let deliveryTypeValueLength = deliveryTypeValue.length;
        deliveryTypeValueLength = 1;
        // const sourceLength = this.uploadedData.length + deliveryTypeValueLength;
        // if (sourceLength < 2) {

        this.entityList = this.allEntities.filter(x => {
            const deliveryTypes = x.sub.split('/');
            // if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
            //   return x;
            // }
            //converting Uppercase coz, while retraining we get Delivery Type name in uppercase from an API response.
            if (deliveryTypes.findIndex(ele => { return ele.toUpperCase() === deliveryName.toUpperCase() }) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
                if (deliveryName === 'PPM' && x.name.indexOf('Iteration') > -1) {
                    return false;
                } else {
                    return x;
                }
            }
        });

        this.entityFormGroup.get('startDateControl').enable();
        this.entityFormGroup.get('endDateControl').enable();
        // this.sourceTotalLength = this.uploadedData.length + deliveryTypeValueLength;
        // }
        // this.disableDownLoadTeamplate = false
    }

    getEntityDetails() {
        this.entityList = [];
        this.allEntities = [];
        this.problemStatementService.getDynamicEntity(this.payloadForGetAICoreModels.clientid,
            this.payloadForGetAICoreModels.dcid, this.payloadForGetAICoreModels.userid).subscribe(
                data => {
                    // this..loadingEnded();
                    this.entityLoader = false;
                    let entitiesArray;
                    Object.entries(data).forEach(
                        ([key, value]) => {
                            entitiesArray = value;
                            entitiesArray.forEach(entity => {
                                const obj = { 'name': entity, 'sub': key };
                                this.allEntities.push(obj);
                            });
                        });

                    // Filtering entity data on the basis of deliveryTypeName
                    /* this.entityList = this.entityList.filter(x => {
                      const deliveryTypes = x.subDeliverable.split('/');
                      if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
                        return x;
                      }
                    }); */

                    if (this.entityList.length === 0) {
                        this.entityFormGroup.get('startDateControl').disable();
                        this.entityFormGroup.get('endDateControl').disable();
                    }
                },
                error => {
                    // this.apputilService.loadingEnded();
                    this.entityLoader = false;
                    this.ns.error('Pheonix metrics api has no data.');
                });
    }

    addStopWords() {

        if (this.stopWordscluster) {
            if (this.clusteringSW.includes(this.stopWordscluster)) {
                this.ns.error('Stop word has been already added to the list');
            } else {
                this.clusteringSW.push(this.stopWordscluster);
            }
            this.stopWordscluster = '';
        }
    }

    removeclusterstopWords(index) {
        this.clusteringSW.splice(index, 1);
    }

    removeEntity() {

        if (this.entityFormGroup) {
            this.entityFormGroup.get('entityControl').setValue('');
            this.entityFormGroup.get('deliveryTypeControl').setValue('');
            this.entityFormGroup.get('startDateControl').setValue('');
            this.entityFormGroup.get('endDateControl').setValue('');

            this.entityFormGroup.get('entityControl').disable();
            if (!(this.env == 'FDS' && (this.requestType == 'AM' || this.requestType == 'IO'))) {
                this.entityFormGroup.get('endDateControl').disable();
                this.entityFormGroup.get('startDateControl').disable();
            } else {
                this.entityFormGroup.get('endDateControl').enable();
                this.entityFormGroup.get('startDateControl').enable();
            }
            this.entityFormGroup.get('deliveryTypeControl').enable();
        }
        let entityArrayLength = this.entityArray.length;
        entityArrayLength = 0;
        this.entityArray = [];
        this.entityArray.length = 0;
        this.provideTrainingDataVolume = '';
        this.selectedOfflineRetrain = false;
        this.selectedRFrequency = '';
        this.rFrequency = '';
        this.selectedRFeqValues = '';
        if (this.offlineRetrainingSelected) {
            this.offlineRetrainingSelected.nativeElement.checked = false;
        }
        // this.removalEntityFlag = true;
        // this.sourceTotalLength = this.files.length + entityArrayLength;
        return false;
    }

    removeDataset() {
        this.datasetEntered = false;
        this.selectedtype = "";
        return false;
    }

    onSubmit() {
        // if (this.sourceTotalLength >= 1) { // Validation - To check if atleast one source is uploaded

        if (this.datasetname === undefined) {
            let entityDeliveryType = null;
            let entityStartDate = null;
            let entityEndDate = null;
            let entityNameVal = null;
            if (this.entityFormGroup) {
                entityDeliveryType = this.entityFormGroup.get('deliveryTypeControl').value;
                entityStartDate = this.entityFormGroup.get('startDateControl').value;
                entityEndDate = this.entityFormGroup.get('endDateControl').value;
                entityNameVal = this.entityFormGroup.get('entityControl').value;
            }
            let isEntityEmpty = false;
            let isDeliveryEmpty = false;
            if (Array.isArray(entityNameVal)) {
                if (entityNameVal.length === 0) {
                    isEntityEmpty = true;
                }
            } else {
                if (this.coreUtilService.isNil(entityNameVal)) {
                    isEntityEmpty = true;
                }
            }

            if (this.coreUtilService.isNil(entityDeliveryType)) {
                isDeliveryEmpty = true;
            }

            let allEntityfieldEmpty = false;
            if ((this.coreUtilService.isNil(entityStartDate) && this.coreUtilService.isNil(entityEndDate)
                && isEntityEmpty === true && isDeliveryEmpty === true)) {
                allEntityfieldEmpty = true;
            }

            if (this.entityView === true && (this.env == 'FDS' || this.env === 'PAM') && (this.requestType == 'AM' || this.requestType == 'IO') && isDeliveryEmpty === true) {
                this.ns.error('Please select Delivery Type');
            }
            else if ((this.coreUtilService.isNil(entityStartDate) || this.coreUtilService.isNil(entityEndDate)
                || isEntityEmpty === true || (isDeliveryEmpty === true && !(this.env == 'FDS' && (this.requestType == 'AM' || this.requestType == 'IO')))) && !allEntityfieldEmpty && this.entityView) {
                // Validation - To check if entity is selected and all the values are filled
                this.ns.error('Fill all the fields for Entity');
            }
            else if (this.provideTrainingDataVolume === '' && this.entityView === true && this.trainingDataVolume) {
                this.ns.error('Please provide training data volume');
            } else { // Forming the payload
                const entityPayload = { 'pad': {} };
                const instaMLPayload = { 'InstaMl': {} };
                const metricsPayload = { 'metrics': {} };
                const filePayload = { 'fileUpload': {} };
                const entitiesNamePayload = { 'EntitiesName': {} };
                const metricsNamePayload = { 'MetricNames': {} };
                const entityObject = {};
                const customPayload = { 'Custom': {} };
                const maxDataforTrainingPayload = { 'MaxDataPull': {} };

                const payload = {
                    'source': undefined,
                    'sourceTotalLength': 1,
                    'parentFileName': undefined,
                    'uploadFileType': 'single',
                    'category': this.selectedDeliveryType
                        ? this.selectedDeliveryType.toUpperCase()
                        : undefined,
                    'mappingFlag': false,
                    'uploadType': 'FileDataSource',
                    'DBEncryption': undefined,
                    'ServiceId': '',
                    'similarity': true,
                    'CorrelationId': '',
                    'languageFlag': this.language
                };

                // TODO :: check reTrainModel flag
                if (this.selectedModelData != undefined && this.reTrainModel == true)
                    payload['CorrelationId'] = this.selectedModelData['CorrelationId'];

                const finalPayload = [];
                let entityArrayLength = this.entityArray.length;

                if (entityArrayLength >= 1) { // Payload for Entity
                    payload.source = 'Entity';
                    let selectedEntityObj = { 'name': '', 'sub': '' };
                    if (this.reTrainModel) {
                        // let entitiesForRetrain = Object.entries(this.selectedModelData.Entities);
                        // selectedEntityObj = { 'name': entitiesForRetrain[0][0], 'sub': entitiesForRetrain[0][1] };
                        this.entityArray.forEach(element => {
                            if (element.name == this.selectedEntity) {
                                selectedEntityObj = element;
                            }
                        });
                    } else {
                        // selectedEntityObj = { 'name': '', 'sub': '' };
                        this.entityList.forEach(element => {
                            if (element.name == this.selectedEntity) {
                                selectedEntityObj = element;
                            }
                        });
                    }

                    if (this.env === 'PAM' || this.env === 'FDS') {
                        customPayload.Custom['startDate'] = this.entityFormGroup.get('startDateControl').value.toLocaleDateString();
                        customPayload.Custom['endDate'] = this.entityFormGroup.get('endDateControl').value.toLocaleDateString();
                        customPayload.Custom['ServiceType'] = entityNameVal;
                        customPayload.Custom['Environment'] = this.env;
                        customPayload.Custom['RequestType'] = this.requestType;
                        payload.source = 'Custom';
                        payload.category = this.requestType;
                    } else {
                        console.log('Selected Entity name' + selectedEntityObj.name);
                        console.log('Selected Entity sub' + selectedEntityObj.sub);
                        entityObject[selectedEntityObj.name] = selectedEntityObj.sub;
                        entityPayload.pad['startDate'] = this.entityFormGroup.get('startDateControl').value.toLocaleDateString();
                        entityPayload.pad['endDate'] = this.entityFormGroup.get('endDateControl').value.toLocaleDateString();
                        entityPayload.pad['Entities'] = entityObject;
                        console.log('selected entity Object', entityObject);
                        entityPayload.pad['method'] = this.selectedDeliveryType ? this.selectedDeliveryType.toUpperCase() : undefined;
                        entitiesNamePayload['EntitiesName'] = selectedEntityObj.name;
                    }
                    maxDataforTrainingPayload['MaxDataPull'] = this.provideTrainingDataVolume;
                    maxDataforTrainingPayload['IsCarryOutRetraining'] = this.selectedOfflineRetrain;
                    maxDataforTrainingPayload['IsOnline'] = this.IsOnline;
                    maxDataforTrainingPayload['IsOffline'] = this.IsOffline;
                    maxDataforTrainingPayload['Retraining'] = { [this.rFrequency]: this.selectedRFeqValues }
                    maxDataforTrainingPayload['Training'] = {};
                    maxDataforTrainingPayload['Prediction'] = {};

                    finalPayload.push(payload);
                    finalPayload.push(entityPayload);
                    finalPayload.push(entitiesNamePayload);
                    finalPayload.push(maxDataforTrainingPayload);
                    finalPayload.push(instaMLPayload);
                    finalPayload.push(metricsPayload);
                    finalPayload.push(metricsNamePayload);
                    if (this.env === "PAM" || this.env === 'FDS') {
                        finalPayload.push(customPayload);
                    }
                    this.Myfiles = finalPayload;

                    if (
                        this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c'
                    ) {
                        if (this.reTrainModel) {
                            this.showModelTemplateName(
                                this.reTrainModel,
                                this.selectedModelCorrelationId
                            );
                        } else {
                            this.ns.success(
                                'Training will be initiated automatically once model is saved.'
                            );
                            this.showModelTemplateName(this.reTrainModel, '');
                        }
                    } else {
                        if (this.reTrainModel) {
                            this.showModelEnterDialog(this.Myfiles, true);
                        } else {
                            this.showModelEnterDialog(this.Myfiles, false);
                        }
                    }
                    // this.dialog.close(finalPayload);
                }
                if (this.uploadedData.length > 0) {
                    // Payload for File
                    payload.source = 'File';
                    // if (this.selectedParentType === undefined) {
                    //   this.selectedParentType = payload.source;
                    // }
                    filePayload.fileUpload['filepath'] = this.uploadedData;
                    finalPayload.push(payload);
                    finalPayload.push(filePayload);
                    finalPayload.push(entityPayload);
                    finalPayload.push(entitiesNamePayload);
                    this.Myfiles = finalPayload;
                    if (
                        this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c'
                    ) {
                        if (this.reTrainModel) {
                            this.showModelTemplateName(
                                this.reTrainModel,
                                this.selectedModelCorrelationId
                            );
                        } else {
                            this.ns.success(
                                'Training will be initiated automatically once model is saved.'
                            );
                            this.showModelTemplateName(this.reTrainModel, '');
                        }
                    } else {
                        if (this.reTrainModel) {
                            this.showModelEnterDialog(this.Myfiles, true);
                        } else {
                            this.showModelEnterDialog(this.Myfiles, false);
                        }
                    }
                    // this.dialog.close(finalPayload);
                }
                if (this.customDataView === true) {// payload for Custom Data
                    if (this.QueryData?.source === CustomDataTypes.Query) {//Payload Custom DB Query.
                        payload.source = CustomDataTypes.Query;
                        payload.DBEncryption = false;
                        const customData = { 'CustomDataPull': this.QueryData.query, 'DateColumn': this.QueryData.DateColumn };

                        finalPayload.push(payload);
                        finalPayload.push(customData);
                        this.Myfiles = finalPayload;
                        if (this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c') {
                            if (this.reTrainModel) {
                                this.showModelTemplateName(
                                    this.reTrainModel,
                                    this.selectedModelCorrelationId
                                );
                            } else {
                                this.ns.success(
                                    'Training will be initiated automatically once model is saved.'
                                );
                                this.showModelTemplateName(this.reTrainModel, '');
                            }
                        } else {
                            if (this.reTrainModel) {
                                this.showModelEnterDialog(this.Myfiles, true);
                            } else {
                                this.showModelEnterDialog(this.Myfiles, false);
                            }
                        }
                    }
                    if (this.apiData?.source === CustomDataTypes.API) {//Payload Custom API.

                        let apiData = this.apiData.apiData;
                        payload.source = CustomDataTypes.API;
                        payload.DBEncryption = apiData[0].PIConfirmation;

                        let AzureCredentials;
                        if (apiData[0].clientid !== undefined && apiData[0].clientsecret !== undefined && apiData[0].resource !== undefined && apiData[0].grantype !== undefined) {
                            AzureCredentials = {
                                'client_id': apiData[0].clientid,
                                'client_secret': apiData[0].clientsecret,
                                'resource': apiData[0].resource,
                                'grant_type': apiData[0].grantype
                            };
                        } else {
                            AzureCredentials = {
                                'client_id': '',
                                'client_secret': '',
                                'resource': '',
                                'grant_type': ''
                            };
                        }

                        var apiFormData = {
                            "startDate": apiData[0].startdate,
                            "endDate": apiData[0].enddate,
                            "SourceType": CustomDataTypes.API,
                            "Data": {
                                "METHODTYPE": apiData[0].HttpMethod,
                                "ApiUrl": apiData[0].Url,
                                "KeyValues": apiData[0].Keyvalue,
                                "BodyParam": apiData[0].Body,
                                "fetchType": apiData[0].fetchType,
                                "Authentication": {
                                    "Type": apiData[0].authttype,
                                    "Token": apiData[0].token,
                                    "AzureUrl": apiData[0].tokenurl,
                                    'AzureCredentials': AzureCredentials,
                                    'UseIngrainAzureCredentials': apiData[0].UseIngrainAzureCredentials
                                },
                                "TargetNode": apiData[1].targetNode
                            }
                        };
                        const customData = { 'CustomDataPull': apiFormData };
                        finalPayload.push(payload);
                        finalPayload.push(customData);
                        this.Myfiles = finalPayload;
                        if (this.selectedServiceId === '50f2232a-0182-4cda-96fc-df8f3ccd216c') {
                            if (this.reTrainModel) {
                                this.showModelTemplateName(
                                    this.reTrainModel,
                                    this.selectedModelCorrelationId
                                );
                            } else {
                                this.ns.success(
                                    'Training will be initiated automatically once model is saved.'
                                );
                                this.showModelTemplateName(this.reTrainModel, '');
                            }
                        } else {
                            if (this.reTrainModel) {
                                this.showModelEnterDialog(this.Myfiles, true);
                            } else {
                                this.showModelEnterDialog(this.Myfiles, false);
                            }
                        }
                    }
                }
            }
        }

        // } else {
        //   this.ns.error('Kindly upload atleast one source of data.');
        // }
        else {
            let correlationId;
            if (this.selectedModelData != undefined && this.reTrainModel == true) {
                correlationId = this.selectedModelData['CorrelationId'];
            }

            let uploadType = '';
            if (this.selectedDataSetSourcetype === 'ExternalAPI') {
                uploadType = 'ExternalAPIDataSet';
            } else if (this.selectedDataSetSourcetype === 'File') {
                uploadType = 'File_DataSet';
            }

            const entityPayload = { pad: {} };
            const entitiesNamePayload = { EntitiesName: {} };
            const DataSetUId = { DataSetUId: this.datasetname };
            const payload = {
                source: 'DataSet',
                sourceTotalLength: 1,
                parentFileName: undefined,
                uploadFileType: 'single',
                category: this.selectedDeliveryType
                    ? this.selectedDeliveryType.toUpperCase()
                    : undefined,
                mappingFlag: false,
                uploadType: uploadType,
                DBEncryption: undefined,
                ServiceId: '',
                similarity: true,
                CorrelationId: correlationId,
                languageFlag: this.language,
            };
            const finalPayload = [];
            finalPayload.push(payload);
            finalPayload.push(entityPayload);
            finalPayload.push(entitiesNamePayload);
            finalPayload.push(DataSetUId);
            //this.dialog.close(finalPayload);
            this.Myfiles = finalPayload;

            if (this.reTrainModel) {
                this.showModelEnterDialog(this.Myfiles, true);
            } else {
                this.showModelEnterDialog(this.Myfiles, false); // this gets executed
            }
        }
    }


    setFileView() {
        this.fileView = true;
        this.entityView = false;
        this.datasetView = false;
        this.trainingDataVolume = false;
        this.customDataView = false;
        this.entityFormGroup = this.formBuilder.group({
            startDateControl: [''],
            endDateControl: [''],
            entityControl: [''],
            deliveryTypeControl: ['']
        });
        this.entityList = [];
        this.provideTrainingDataVolume = '';
        this.selectedOfflineRetrain = false;
        this.datasetname = undefined;
        this.clearCustomData();
    }

    setDatasetView() {
        this.datasetView = true;
        this.fileView = false;
        this.entityView = false;
        this.trainingDataVolume = false;
        this.customDataView = false;
        this.entityFormGroup = this.formBuilder.group({
            startDateControl: [''],
            endDateControl: [''],
            entityControl: [''],
            deliveryTypeControl: [''],
        });
        this.entityList = [];
        this.provideTrainingDataVolume = '';
        this.clearCustomData();
    }

    setCustomDataView() {
        this.customDataView = true;
        this.datasetView = false;
        this.fileView = false;
        this.entityView = false;
        this.trainingDataVolume = false;
        this.entityFormGroup = this.formBuilder.group({
            startDateControl: [''],
            endDateControl: [''],
            entityControl: [''],
            deliveryTypeControl: [''],
        });
        this.entityList = [];
        this.provideTrainingDataVolume = '';
        this.loadCustomDataView();
        this.clearCustomData();
    }

    deletewordcloudmodel(model) {
        this.isDelete = true;
        this.problemStatementService.deletewordcloud(model.CorrelationId).subscribe(response => {
            if (response) {
                this.isDelete = false;
                this.getCusteringModels();
                this.ns.success('Model has been deleted');
            } else {
                this.isDelete = false;
                this.ns.error(response);
            }
        }, error => {
            this.isDelete = false;
            this.ns.error(error.error);
        });
    }

    allowDrop(event) {
        event.preventDefault();
    }

    onDrop(event) {
        event.preventDefault();

        for (let i = 0; i < event.dataTransfer.files.length; i++) {
            this.uploadedData.push(event.dataTransfer.files[i]);
        }
    }

    checkCorrelationId(id: string) {
        if (id !== this.selectedModelCorrelationId) {
            this.ns.error('Kindly select a model to train/re-train.');
            return false;
        } else {
            return true;
        }
        //  else {
        //   return false;
        // }
    }

    setClusterType(toogle) {
        if (toogle.checked) {
            this.selectClusteringType('Non-Text');
        } else {
            this.selectClusteringType('Text');
        }
    }

    showVisualizationText(detail) {
        if (this.clusterChartTextView) {
            this.clusterChartTextView = false;
        } else {
            const payload = {
                'CorrelationId': this.selectedModelCorrelationId,
                'ClientID': this.paramData.clientUID,
                'DCUID': this.paramData.deliveryConstructUId,
                'mapping': this.clusterMapping.ClusterInputs.KMeans,
                'SelectedModel': 'KMeans'
            };

            if (this.selectedModelsForEval.DBSCAN.Train_model === 'True') {
                payload.mapping = this.clusterMapping.ClusterInputs.DBSCAN;
                payload.SelectedModel = 'DBSCAN';
            }

            this._appUtilsService.loadingImmediateEnded();
            this._appUtilsService.loadingStarted();
            this.selectedModel = this.pageloadCluster.clusteringStatus.find(data => this.selectedModelCorrelationId === data.CorrelationId);
            if (this.selectedModel?.ingestData?.mapping && Object.keys(this.selectedModel?.ingestData?.mapping).length && !this.currentSuggestion) {
                this.ns.error('No changes have made');
                this._appUtilsService.loadingEnded();
            } else if (this.selectedModel?.ingestData?.mapping && Object.keys(this.selectedModel?.ingestData?.mapping).length && this.currentSuggestion) {
                var _this = this;
                const suggestionResult = Object.keys(this.selectedModel?.ingestData?.mapping).every(key => {
                    return _this.selectedModel?.ingestData?.mapping[key] === _this.currentSuggestion[key];
                });
                if (!suggestionResult) {
                    this.problemStatementService.getVisualizationData(payload).subscribe(
                        (result) => {
                            if (result && result.Message === 'Success') {
                                this.getVisualizationStatus('text');
                            } else {
                                this.ns.error(result.Message);
                                this._appUtilsService.loadingEnded();
                            }
                        }, (error) => {
                            this.ns.error('Something went wrong');
                            this._appUtilsService.loadingEnded();
                        }
                    );
                } else {
                    this.ns.error('No changes have made');
                    this._appUtilsService.loadingEnded();
                }
            } else {
                this.problemStatementService.getVisualizationData(payload).subscribe(
                    (result) => {
                        if (result && result.Message === 'Success') {
                            this.getVisualizationStatus('text');
                        } else {
                            this.ns.error(result.Message);
                            this._appUtilsService.loadingEnded();
                        }
                    }, (error) => {
                        this.ns.error('Something went wrong');
                        this._appUtilsService.loadingEnded();
                    }
                );
            }
        }
    }

    graphicalTextVisualizationView(datafromapi?) {
        this.treeMapData = null;
        let data = {
            'Olives': 4319,
            'Tea': 4159,
            'Mashed Potatoes': 2583,
            'Boiled Potatoes': 2074,
            'Milk': 1894,
            'Chicken Salad': 1809,
            'Vanilla Ice Cream': 1713,
            'Cocoa': 1636,
            'Lettuce Salad': 1566,
            'Lobster Salad': 1511,
            'Chocolate': 1489,
            'Chocolate2': 1489,
            'Chocolate3': 1489,
            'Chocolate4': 1489,
            'Chocolate5': 1489
        }
        if (datafromapi) {
            data = datafromapi;
        }
        this.treeMapData = { 'children': this.convertIntoArray(data['Frequency_Count']) };
        this.setOfWordCloud = data['Visualization_Response'];
    }

    showVisualizationNonText() {
        if (this.clusterChartNonTextView) {
            this.clusterChartNonTextView = false;
        } else {
            const payload = {
                'CorrelationId': this.selectedModelCorrelationId,
                'ClientID': this.paramData.clientUID,
                'DCUID': this.paramData.deliveryConstructUId,
                'mapping': this.clusterMapping.ClusterInputs.KMeans,
                'SelectedModel': 'KMeans'

            };

            if (this.selectedModelsForEval.DBSCAN.Train_model === 'True') {
                payload.mapping = this.clusterMapping.ClusterInputs.DBSCAN;
                payload.SelectedModel = 'DBSCAN';
            }

            // const message =   this.clusterMapping.ClusterInputs.KMeans['Cluster ' + index];
            this._appUtilsService.loadingImmediateEnded();
            this._appUtilsService.loadingStarted();
            this.problemStatementService.getVisualizationData(payload).subscribe(
                (result) => {
                    if (result && result.Message === 'Success') {
                        this.getVisualizationStatus('non-text');
                    } else {
                        this.ns.error(result.Message);
                        this._appUtilsService.loadingEnded();
                    }
                },
                (error) => {

                }
            );
        }
    }

    graphicalNonTextVisualizationView(datafromapi) {

        this.bubbleChartData = null;
        this.barGroupData = null;

        let data = datafromapi;

        if (datafromapi) {
            data = datafromapi;
        }
        if (data.Frequency_Count) {
            this.bubbleChartData = { 'children': this.convertIntoArray(data.Frequency_Count) };
        }

        this.barGroupData = data.Visualization_Response;
    }


    getVisualizationStatus(clusteringType) {
        let selectedModel = 'KMeans';
        if (this.selectedModelsForEval.DBSCAN.Train_model === 'True') {
            selectedModel = 'DBSCAN';
        }
        this.ingestStatusSubscription = this.problemStatementService.getVisulalisationDataStatus(this.selectedModelCorrelationId,
            selectedModel).subscribe(
                (data) => {
                    if (data['Status'] === 'P' || data['Status'] === null || data['Status'] === 'New') {
                        this.retryVisualization(clusteringType);
                    } else if (data['Status'] === 'C') {
                        const params = {
                            'clientid': this.paramData.clientUID,
                            'dcid': this.paramData.deliveryConstructUId,
                            'serviceid': this.selectedServiceId,
                            'userid': this._appUtilsService.getCookies().UserId
                        };
                        this.problemStatementService.getCusteringModels(params).subscribe(models => {
                            this.backUpClusteringModels = models;
                            this.pageloadCluster = _.cloneDeep(models);
                            this._appUtilsService.loadingImmediateEnded();
                            if (data.Visualization_Response && clusteringType === 'text') {
                                this.clusterChartTextView = true;
                                this.graphicalTextVisualizationView(data);
                            } else if (data && data.Visualization_Response) {
                                this.clusterChartNonTextView = true;
                                this.graphicalNonTextVisualizationView(data);
                            }
                        });
                        // this.getCusteringModels();
                    } else if (data['Status'] === 'E') {
                        this._appUtilsService.loadingImmediateEnded();
                        this.ns.error(data['Message']);
                    } else {
                        this.ns.error(`Error occurred: Due to some backend data process
       the relevant data could not be produced. Please try again while we troubleshoot the error`);
                        this.unsubscribe();
                        this._appUtilsService.loadingImmediateEnded();
                    }
                },
                (error) => {
                    this.ns.error(`Something went wrong`);
                    this.unsubscribe();
                    this._appUtilsService.loadingImmediateEnded();
                }
            )
    }

    retryVisualization(clusteringType) {
        this.timerSubscription = timer(10000).subscribe(() =>
            this.getVisualizationStatus(clusteringType));
        return this.timerSubscription;
    }

    convertIntoArray(jsonFormat) {
        let s = {};
        let a = [];
        let ob = Object.keys(jsonFormat);
        for (let i = 0; i < ob.length; i++) {
            s = { 'Name': '', 'Count': 0 };
            s['Name'] = ob[i];
            s['Count'] = jsonFormat[ob[i]];
            a.push(s);
        }
        return (a.length > 0 ? a : null);
    }

    selectedchoosetype(value) {
        this.datasetname = value;
        const index = this.choosetype.findIndex(x => x.id === value);
        this.datasetname2 = this.choosetype[index].name;
    }

    selectedchoosesource(source) {
        this.choosetype = [];
        // let sourcetype;
        if (source === 'API') {
            this.selectedDataSetSourcetype = 'ExternalAPI';
        } else {
            this.selectedDataSetSourcetype = source;
        }
        this.getdatasetdetails(this.selectedDataSetSourcetype);
    }

    getdatasetdetails(sourcetype) {
        let i = 0;
        this.problemStatementService
            .showdatasetdropdown(
                this.paramData.clientUID,
                this.paramData.deliveryConstructUId,
                this._appUtilsService.getCookies().UserId
            )
            .subscribe((data) => {
                data.forEach((element) => {
                    if (element['SourceName'] === sourcetype) {
                        const datasets = {
                            name: element['DataSetName'],
                            id: element['DataSetUId'],
                        };
                        this.choosetype.push(datasets);
                        this.datasetArray[i++] = datasets.name;
                    }
                });
            });
    }

    checkColor() {
        if (!this.enablePublishUseCaseTab) {
            const styles = {
                'background-color': '#000088',
                'color': 'white'
            };
            return styles;
        } else {
            const styles = {
                'background-color': 'white',
                'color': 'black'
            };
            return styles;
        }
    }

    checkColorPublish() {
        if (this.enablePublishUseCaseTab) {
            const styles = {
                'background-color': '#000088',
                'color': 'white'
            };
            return styles;
        } else {
            const styles = {
                'background-color': 'white',
                'color': 'black'
            };
            return styles;
        }
    }

    downloadPrediction() {
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    self.getprediction();
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                self.getprediction();
            });
        }
    }

    getprediction() {
        const self = this;
        if (self.noofpage == undefined || self.noofpage == 1) {
            self.jsondata = {
                'CorrelationId': self.selectedModelCorrelationId,
                'UniqueId': self.selectedUniId,
                'PageNumber': 1,
                'Bulk': 'Bulk'
            };
        } else {
            self.jsondata = {
                'CorrelationId': self.selectedModelCorrelationId,
                'UniqueId': self.selectedUniId,
                'PageNumber': self.noofpage,
                'Bulk': 'Bulk'
            };
        }

        self._appUtilsService.loadingStarted();
        self.problemStatementService.getBulkPrediction(self.jsondata).subscribe((data) => {
            if (data.Status.toLowerCase() === 'error') {
                this._appUtilsService.loadingImmediateEnded();
                this.ns.error(data.StatusMessage);
                return;
            }
            let keys = Object.keys(data.PredictedData);
            self.totalPageCount = data.TotalPageCount;
            self.noofpage = data.PageNumber;
            for (let i = 0; i < keys.length; i++) {
                self.bulkData[keys[i]] = data.PredictedData[keys[i]];
            }
            if (self.noofpage < self.totalPageCount) {
                //  self._appUtilsService.loadingImmediateEnded();
                self.noofpage = self.noofpage + 1;
                self.retryjson(self.noofpage, self.totalPageCount);
            } else if (self.noofpage == self.totalPageCount) {
                const blob = new Blob([JSON.stringify(self.bulkData, null, 4)], { type: 'application/json' });
                if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
                    saveAs(blob, 'BulkTemplate.json');
                    self.bulkData = {};
                    self.noofpage = undefined;
                    self.ns.success('File Downloaded Succesfully');
                    self._appUtilsService.loadingImmediateEnded();
                } else {
                    self._excelService.downloadPasswordProtectedZIPJSON(blob, 'BulkTemplate.json').subscribe(data => {
                        let binaryData = [];
                        binaryData.push(data);
                        let downloadLink = document.createElement('a');
                        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                        downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
                        document.body.appendChild(downloadLink);
                        downloadLink.click();
                        self.ns.success('Downloaded Data Successfully');
                        self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                        self.bulkData = {};
                        self.noofpage = undefined;
                        self._appUtilsService.loadingImmediateEnded();
                    }, (error) => {
                        self.ns.error(error);
                    });
                }
            }
        }, (error) => {
            this.ns.error(`Something went wrong`);
            this._appUtilsService.loadingImmediateEnded();
        });
    }

    retryjson(noofpage, totalPageCount) {
        if (noofpage <= totalPageCount) {
            this.timerSubscription = timer(1000).subscribe(() => this.getprediction());
        }


    }

    searchModel(searchText) {
        if (this.enableModelTab) {
            this.trainedModels = this.coreModlesList;
            if (!this.coreUtilService.isNil(searchText)) {
                this.trainedModels = _.filter(this.coreModlesList, function (item) {
                    return item.ModelName.indexOf(searchText) > -1;
                });
            }
        } else if (this.enablePublishUseCaseTab) {
            this.publishUseCases = this.corePublishModelList;
            if (!this.coreUtilService.isNil(searchText)) {
                this.publishUseCases = _.filter(this.corePublishModelList, function (item) {
                    return item.UsecaseName.indexOf(searchText) > -1;
                });
            }
        }
    }

    closeSearchBox() {
        this.searchInput = '';
        if (this.enableModelTab) {
            this.trainedModels = this.coreModlesList;
        } else if (this.enablePublishUseCaseTab) {
            this.publishUseCases = this.corePublishModelList;
        }
    }

    onValueChange(query: string) {
        this.textChanged.next(query);
    }

    isSpecialCharacterMaxData(input) {
        const regex = /^[0-9 ]+$/;
        //console.log(input);
        const isValid = regex.test(input);
        this.provideTrainingDataVolume = input;
        if (input && input.length > 0) {
            if (!isValid) {
                this.ns.error('Enter only numeric value');
                this.isValidDataVolume = false;
                return 0;
            } else {
                if (input < 4) {
                    this.ns.error('The min limit on data pull is 4 records. Please validate the value.');
                    this.isValidDataVolume = false;
                    return 1;
                }
                else if (input > 30000) {
                    this.ns.error('The max limit on data pull is 30000 records. Please validate the value.');
                    this.isValidDataVolume = false;
                } else {
                    this.isValidDataVolume = true;
                }
            }
        } else {
            this.isValidDataVolume = false;
        }
    }

    verifyAdminUser() {
        this._appUtilsService.getRoleData().subscribe(userDetails => {
            const logedinUserRole = userDetails.accessRoleName;
            if (logedinUserRole === 'System Admin' || logedinUserRole === 'Client Admin' ||
                logedinUserRole === 'System Data Admin' || logedinUserRole === 'Solution Architect') {
                this.isAdminUser = true;
            } else {
                this.isAdminUser = false;
            }
        });
    }

    loadCustomDataView() {
        this._modalService.show(CustomDataViewComponent, { class: 'modal-dialog modal-xl', backdrop: 'static', initialState: { title: 'Custom Data', selectedServiceName: this.selectedServiceName, ServiceId: this.selectedServiceId } }).content.Data.subscribe(data => {
            console.log(data);
            if (data.source === CustomDataTypes.API) {
                this.apiData = data;
            }
            if (data.source === CustomDataTypes.Query) {
                this.QueryData = data;
            }
            this.onSubmit();
        });
    }

    clearCustomData() {
        this.apiData = undefined;
        this.QueryData = undefined;
    }

    // code to save custom configuration Region - starts.
    saveCustomConfiguration(resData) {
        const payload = {
            "ApplicationID": resData.ApplicationId,
            "CorrelationID": resData.CorrelationId,
            "userId": this.userId,
            "clientUID": this.paramData.clientUID,
            "deliveryUID": this.paramData.deliveryConstructUId,
            "ModelName": resData.ModelName,
            // "ModelType": this.problemType,
            "DataSource": resData.SourceName,
            "ServiceId": this.selectedServiceId,
            "UsecaseName": resData.UsecaseName,
            "TemplateUseCaseID": null,
            "UseCaseID": resData.UsecaseId
        }

        this._deploymodelService.saveCustomConfiguration(payload, serviceLevel.AI).subscribe(response => {
            this.ns.success('Custom Configuration saved successfully.');
            this._deploymodelService.clearCustomConfigStorage();
        }, error => {
            this.ns.error('something Went Wrong while saving Custom Configurations.');
            this._deploymodelService.clearCustomConfigStorage();
        });

    }
    // code to save custom configuration Region - ends.

    GetSimilarityRecordCount(correlationId) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.GetSimilarityRecordCount(correlationId).subscribe(message => {
            this.RecordsCountMessage = message;
            this.enableRecordsCountView = true;
            this._appUtilsService.loadingEnded();
        }, error => {
            this._appUtilsService.loadingEnded();
        });
    }

    GetClusteringRecordCount(correlationId, uploadType, dataSetUId) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.GetClusteringRecordCount(correlationId, uploadType, dataSetUId).subscribe(message => {
            this.RecordsCountMessage = message;
            this.enableRecordsCountView = true;
            this._appUtilsService.loadingEnded();
        }, error => {
            this._appUtilsService.loadingEnded();
        });
    }

    getRecordCount(model) {
        if (this.isClustering !== true && this.isWordCloud !== true) {
            this.GetSimilarityRecordCount(model.CorrelationId);
        } else {
            this.GetClusteringRecordCount(model.CorrelationId, model.DataSource, model.ingestData.DataSetUId);
        }
    }

    fillInputFields(data) {
        this.selectedDeliveryType = "AIops";
        this.entityFormGroup.patchValue({ deliveryTypeControl: "AIops" });
        this.entityFormGroup.get('startDateControl').enable();
        this.entityFormGroup.get('endDateControl').enable();
        this.entityArray[0] = this.selectedEntity = data.ServiceType;
        this.entityFormGroup.patchValue({ startDateControl: new Date(data.StartDate), endDateControl: new Date(data.EndDate), entityControl: data.ServiceType });
    }
}


