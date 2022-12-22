import { Component, OnInit, ElementRef, Output, EventEmitter, AfterViewInit, ViewChild, TemplateRef, Input } from '@angular/core';
import { Validators, FormBuilder, FormGroup } from '@angular/forms';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { throwError } from 'rxjs';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { isArray } from 'util';
import { ProblemStatementService } from '../../_services/problem-statement.service';
import { ExcelService } from 'src/app/_services/excel.service';
import { HttpClient } from '@angular/common/http';
import * as _ from 'lodash';
import { DialogService } from 'src/app/dialog/dialog.service';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ApiService } from 'src/app/_services/api.service';
import * as $ from 'jquery';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { serviceLevel } from 'src/app/_enums/service-types.enum';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

declare var userNotification: any;

type TypeOfUpload = 'Multiple' | 'Single' | null;
type TypeOfData = 'Metric' | 'Entity' | 'File' | 'Dataset' | 'API' | 'CustomData';
type TypeOfOption = 'AddQuery' | 'API' | 'Filters';

const templateFileDetails = [
    {
        "fileName": "Project_Health_Check.xlsx",
        "modelName": "Proactive Alert on Project Health Status"
    },
    {
        "fileName": "RCA.xlsx",
        "modelName": "Root Cause Recommendation"
    },
    /* Agile */
    {
        "fileName": "Capacity based Velocity Predictor using Actual Efforts.xlsx",
        "modelName": "Capacity based Velocity Predictor Actual effort" // "Capacity based Velocity Predictor using Actual Efforts"
    },
    {
        "fileName": "Capacity based Velocity Predictor using Planned Efforts.xlsx",
        "modelName": "Capacity based Velocity Predictor using Planned effort" // "Capacity based Velocity Predictor using Planned Efforts"
    },
    {
        "fileName": "Post Release Defect Prediction.xlsx",
        "modelName": "Post  Release Defect Prediction" // "Post Release Defect Prediction"
    },
    {
        "fileName": "Predict Sprint Velocity.xlsx",
        "modelName": "Team Level Sprint Velocity Prediction" // "Predict Sprint Velocity"
    },
    {
        "fileName": "Predict Story Points.xlsx",
        "modelName": "Story Point Prediction" // "Predict Story Points"
    },
    {
        "fileName": "UserStory Priority Prediction.xlsx",
        "modelName": "UserStory Priority Prediction"
    },
    {
        "fileName": "Velocity Prediction Time Series.xlsx",
        "modelName": "Velocity Predictor Time series" // "Velocity Prediction Time Series"
    },
    /* DevOps */
    {
        "fileName": "Technical Debt.xlsx",
        "modelName": "Technical Debt Prediction" // Predict Techical Debt
    },
    {
        "fileName": "Developer File Recommendation.xlsx",
        "modelName": "Developer Prediction -  File Updated" // "Developer File Recommendation"
    },
    {
        "fileName": "Overall Release Confidence Prediction.xlsx",
        "modelName": "Overall Release Confidence Prediction"
    },
    {
        "fileName": "Overall Release Quality Prediction.xlsx",
        "modelName": "Overall Release Quality Prediction"
    },

    /* AIOps */
    {
        "fileName": "Incident SLA Resolution Prediction.xlsx",
        "modelName": "Incident SLA Resolution Prediction App" // Incident SLA Resolution Prediction    
    },
    {
        "fileName": "Service Request SLA Resolution Prediction Infra.xlsx",
        "modelName": "Service Request SLA Resolution Prediction Infra" // Service Request SLA Resolution Prediction Infra
    },
    {
        "fileName": "Customer Satisfaction Score Prediction.xlsx",
        "modelName": "Predict Customer Satisfaction Score" // Customer Satisfaction Score Prediction 
    },
    {
        "fileName": "Change Request Success Prediction.xlsx",
        "modelName": "Change Request Success Prediction"
    },
    {
        "fileName": "Recommend Change Request Expedition.xlsx",
        "modelName": "Recommend Change Request Expedition" // Recommend Change Request Expedition  
    },
    {
        "fileName": "Recommend Target Segment.xlsx",
        "modelName": "Recommend Target Segment"
    },
    {
        "fileName": "Forecast Incident Inflow.xlsx",
        "modelName": "Forecast Incident Inflow"
    },
    {
        "fileName": "Incident Resolution Time Prediction.xlsx",
        "modelName": "Incident Resolution Time Prediction App"
    },
    {
        "fileName": "Recommend Incident Support Group.xlsx",
        "modelName": "Recommend Incident Support Group App"
    },
    {
        "fileName": "Incident Resolution Effort Prediction.xlsx",
        "modelName": "Incident Resolution Effort Prediction App"
    },
    {
        "fileName": "Recommend Incident Category.xlsx",
        "modelName": "Recommend Incident Category App"
    },
    {
        "fileName": "Recommend Service Request Category.xlsx",
        "modelName": "Recommend Service Request Category Infra" // Root Cause Recommendation    
    },
    {
        "fileName": "Recommend Service Request Support Group.xlsx",
        "modelName": "Recommend Service Request Support Group Infra"
    },
    {
        "fileName": "Service Request Resolution Effort Prediction.xlsx",
        "modelName": "Service Request Resolution Effort Prediction Infra" // Root Cause Recommendation    
    },
    {
        "fileName": "Service Request Resolution Time Prediction.xlsx",
        "modelName": "Service Request Resolution Time Prediction Infra"
    }
];

@Component({
    selector: 'app-upload-api',
    templateUrl: './upload-api.component.html',
    styleUrls: ['./upload-api.component.scss']
})
export class UploadApiComponent implements OnInit, AfterViewInit {
    @ViewChild('fileInput', { static: false }) fileInput;
    @Output() uploadedData = new EventEmitter<any>();
    @Input() deliveryTypeName : string;
    @Input() previousQuery : string;
    @Input() preBuiltModelName : string;
    @Input() useCaseDefinition : boolean;
    @Input() Flag : boolean;
    @Input() clientUID : string;
    @Input() deliveryConstructUID : string;
    @Input() userId : string;
    @Input() fromApp : string;
    @Input() filteredMetricData = [];
    correlationId: any;
    Myfiles: any;
    modelName: any;
    datasetname;
    // entityList = ['Defect', 'Task', 'Requirement', 'Milestone', 'Deliverable'];
    entityList = [];
    metricsList = [];
    subEntityList = ['Release', 'Sprint', 'Iteration', 'Phase', 'Workplan'];
    selectedEntity;
    selectedSubEntity;
    startDate = new Date();
    apiRow: FormGroup;
    metricRow: FormGroup;
    files = [];
    uploadStatus: string;
    entityCheckbox = false;
    metricsCheckbox = false;
    // openUseCaseDefination: boolean;
    uploadFilesCount = 0;
    selectedParentFile = [];
    entityArray = [];
    metricsArray = [];
    sourceTotalLength: number;
    allowedExtensions = ['.xlsx', '.csv'];
    metricIterationName = [];
    selectedMetricIterationName;
    iterationTypeUId;
    selectedParentType;
    metricCount = 0;
    metricsText = '';
    env;
    requestType;
    entityVal;
    PIConfirmation;
    prebuiltFlag: boolean;
    removalEntityFlag: boolean;
    EntityData: string;
    entityselection: boolean;
    entityLoader = 'apiPending'; // Monitor the Entity API Calling Time
    metricsLoader = 'apiPending';  // Monitor the Entity API Calling Time
    metricData: any;
    agileRow: FormGroup;
    agiletoggle: FormGroup;
    agileFlag: boolean;
    ToggleSwitch: any;
    FileUploadFlag: boolean;
    tableDataforDownload;
    entityStartDate;
    entityEndDate;
    language: string;
    dataFabricOption;
    typeOfUpload: TypeOfUpload = 'Single';
    typeOfDataForSource1: TypeOfData = 'File';
    typeOfDataForSource2: TypeOfData;
    fileUploadView = true;
    entityMetricView = false;
    datasetview: boolean = false;
    apiview: boolean = false;
    choosetype = [];
    selectedsource;
    choosesource = [{ 'name': 'File', 'value': 'File' },
    { 'name': 'API', 'value': 'API' }];

    addQueryView: boolean = true;
    APIView: boolean = false;
    isQueryButtonEnabled: boolean = true;
    queryResponse = [];
    isAdminUser: boolean = false;
    customDataView: boolean = false;
    displayedColumns: string[];
    dataSource: any[];
    ApiConfig: any;
    serviceLevel = serviceLevel.SSAI;

    constructor(private ns: NotificationService, private formBuilder: FormBuilder,
        private coreUtilsService: CoreUtilsService,
        private problemStatementService: ProblemStatementService,
        private _excelService: ExcelService, public modalRef: BsModalRef, private dialogService: DialogService, private environmentService: EnvironmentService,
        private api: ApiService, private appUtilsService: AppUtilsService) {
        if (localStorage.getItem('CustomFlag')) {
            this.agileFlag = true;

        } else {
            this.agileFlag = false;
        }
    }
    releaseSuccessPredictor = false;

    templateName = '';
    showDownloadOption = false;
    filteredDownloadTemp;
    filenameToDownload;

    downloadTempURL = 'src/assets/files/ModelTemplates/DownloadTemplateFileInfo/prebuildDownloadTemplate.json';
    instanceType: string;

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        if (this.environmentService.IsPADEnvironment()) {//enable custom data option only for PAD environmant
            this.verifyAdminUser();
        }
        this.api.openDisclaimer();
        this.env = sessionStorage.getItem('Environment');
        this.requestType = sessionStorage.getItem('RequestType');
        this.removalEntityFlag = false;
        this.entityselection = false;
        this.ToggleSwitch = 'True';
        if (this.agileFlag === true) {
            this.FileUploadFlag = true;
        }
        if (sessionStorage.getItem('Language')) {
            this.language = sessionStorage.getItem('Language').toLowerCase();
        } else {
            this.language = 'english';
        }
        this.templateName = this.preBuiltModelName;

        if (this.deliveryTypeName !== undefined) {
            let deliveryName;
            if (this.deliveryTypeName === 'Global') {
                deliveryName = 'Devops';
            } else {
                deliveryName = this.deliveryTypeName;
            }
            if ((this.env === 'PAM' || this.env === 'FDS') && this.requestType === 'AM') {
                this.entityLoader = 'apiDone';
                this.entityList = [{ 'name': 'Incidents', 'subDeliverable': 'ALL' },
                { 'name': 'ServiceRequests', 'subDeliverable': 'ALL' },
                { 'name': 'ProblemTickets', 'subDeliverable': 'ALL' },
                { 'name': 'WorkRequests', 'subDeliverable': 'ALL' },
                { 'name': 'CHANGE_REQUEST', 'sub': 'ALL' }];
            } else if ((this.env === 'PAM' || this.env === 'FDS') && this.requestType === 'IO') {
                this.entityLoader = 'apiDone';
                this.entityList = [{ 'name': 'Incidents', 'subDeliverable': 'ALL' },
                { 'name': 'ServiceRequests', 'subDeliverable': 'ALL' },
                { 'name': 'ProblemTickets', 'subDeliverable': 'ALL' },
                { 'name': 'ChangeRequests', 'subDeliverable': 'ALL' }];
            } else {
                this.getEntityDetails(deliveryName);
            }
        }

        this.apiRow = this.formBuilder.group({
            //  apiSelect: [''],
            startDateControl: [''],
            endDateControl: [''],
            entityControl: [''],
            subEntityControl: [''],
        });

        this.metricRow = this.formBuilder.group({
            metricStartDateControl: [''],
            metricEndDateControl: [''],
            //   metricsIntervalControl : [this.selectedMetricTimeInterval],
            metricsIterationControl: [],
            metricsControl: [''],
        });

        // agile Navigation
        this.agileRow = this.formBuilder.group({
            //  apiSelect: [''],
            agilestartDateControl: [''],
            agileendDateControl: [''],
        });

        this.agiletoggle = this.formBuilder.group({
            //  apiSelect: [''],
            toggle: ['']
        });

        let entityArrayLength = this.entityArray.length;
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            entityArrayLength = 0;
        }
        this.sourceTotalLength = this.files.length + entityArrayLength + this.metricCount;
        this.fileUploadView = true;
        if (!this.coreUtilsService.isNil(this.useCaseDefinition)) {
            this.fileUploadView = this.useCaseDefinition;
        }

        this.selectedsource = 'File';
        this.getdatasetdetails(this.selectedsource);

        if (!this.coreUtilsService.isNil(this.Flag)) {
            this.prebuiltFlag = this.Flag;
        }

        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')
            && localStorage.getItem('oldCorrelationID') !== null) {
            if (this.prebuiltFlag) {
                const correlationId = localStorage.getItem('oldCorrelationID');
                this.problemStatementService.GetDefaultEntityName(correlationId).subscribe(
                    data => {
                        if (data.includes('.xlsx') === false && data.includes('.csv') === false && data !== '') {
                            this.EntityData = data;
                            this.apiRow.get('entityControl').setValue(this.EntityData);
                        }
                        // this.entityname = this.EntityData;
                    },
                    error => {
                        // this.apputilService.loadingEnded();
                        this.ns.error('Something went wrong.');
                    });
            }
        }
        if (localStorage.getItem('oldCorrelationID') === 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6') {
            this.releaseSuccessPredictor = true;
        } else {
            this.releaseSuccessPredictor = false;
        }
        let prebuildModelName = this.templateName?.toString().trim();
        this.filteredDownloadTemp = _.map(templateFileDetails, function (o) {
            if ((o.modelName.toLowerCase()) == (prebuildModelName?.toLocaleLowerCase()))
                return o;
        });

        this.filteredDownloadTemp = _.without(this.filteredDownloadTemp, undefined)
        if (this.filteredDownloadTemp.length > 0) {
            this.showDownloadOption = true;
            this.filenameToDownload = this.filteredDownloadTemp[0].fileName;
        } else {
            this.showDownloadOption = false;
        }
        // console.log('----------------', this.filteredDownloadTemp);
    }

    ngAfterViewInit() { }

    toggleRowContent(e) {
        // text
        const row = e.target.parentNode.parentNode.parentNode.parentNode.parentNode;
        if (row.classList.contains('open')) {
            row.classList.remove('open');
        } else {
            row.classList.add('open');
        }
    }

    onSubmit() {

        if (this.datasetname === undefined && this.typeOfDataForSource2 !== 'Dataset') {
            if (this.sourceTotalLength >= 1) { // Validation - To check if atleast one source is uploaded
                /* const uploadType = this.apiRow.get('apiSelect').value;
                  if (uploadType === '' && this.sourceTotalLength !== 1) { // Validation - To check if parent or multiple source option is selected
                   this.ns.error('Kindly select parent or multiple source option.');
                 } else */
                const entityStartDate = this.apiRow.get('startDateControl').value;
                const entityEndDate = this.apiRow.get('endDateControl').value;
                const metricStartDate = this.metricRow.get('metricStartDateControl').value;
                const metricEndDate = this.metricRow.get('metricEndDateControl').value;
                const entityNameVal = this.apiRow.get('entityControl').value;
                /* // below line added from PAM branch after sanity remove if not required
                if (this.files.length >= 1) {
                  entityNameVal = [];
                } else {
                  entityNameVal = this.apiRow.get('entityControl').value;
                }
                */
                let isEntityEmpty = false;
                if (Array.isArray(entityNameVal)) {
                    if (entityNameVal.length === 0) {
                        isEntityEmpty = true;
                    }
                } else {
                    if (this.coreUtilsService.isNil(entityNameVal)) {
                        isEntityEmpty = true;
                    }
                }
                let allEntityfieldEmpty = false;
                if ((this.coreUtilsService.isNil(entityStartDate) && this.coreUtilsService.isNil(entityEndDate)
                    && isEntityEmpty === true)) {
                    allEntityfieldEmpty = true;
                }
                const metricNameVal = this.metricRow.get('metricsControl').value;
                let isMetricEmpty = false;
                if (Array.isArray(metricNameVal)) {
                    if (metricNameVal.length === 0) {
                        isMetricEmpty = true;
                    }
                } else {
                    if (this.coreUtilsService.isNil(metricNameVal)) {
                        isMetricEmpty = true;
                    }
                }
                let allMetricfieldEmpty = false;
                if ((this.coreUtilsService.isNil(metricStartDate) && this.coreUtilsService.isNil(metricEndDate)
                    && isMetricEmpty === true)) {
                    allMetricfieldEmpty = true;
                }
                if (!this.selectedParentFile.length && this.sourceTotalLength !== 1) {
                    this.ns.error('Please select parent file.'); // Validation - To check if any parent source is selected
                } else if ((this.coreUtilsService.isNil(entityStartDate) || this.coreUtilsService.isNil(entityEndDate)
                    || isEntityEmpty === true) && !allEntityfieldEmpty) {
                    // Validation - To check if entity is selected and all the values are filled
                    //throwError('Fill all the mandatory fields for Entity');
                    this.ns.error('Fill all the mandatory fields for Entity');
                } else if ((this.coreUtilsService.isNil(metricStartDate) || this.coreUtilsService.isNil(metricEndDate)
                    || isMetricEmpty === true) && !allMetricfieldEmpty) {
                    // Validation-To check if metric is selected & all the value are filled
                    // throwError('Fill all the mandatory fields for Metrics');
                    this.ns.error('Fill all the mandatory fields for Metrics');
                } else if (this.files.length >= 1 && this.PIConfirmation === undefined) {
                    // Validation for PI data confirmation
                    this.ns.error('Kindly select the PII data confirmation options.');
                } else { // Forming the payload
                    const entityPayload = { 'pad': {} };
                    const instMLPayload = { 'InstaMl': {} };
                    const metricsPayload = { 'metrics': {} };
                    const filePayload = { 'fileUpload': {} };
                    const entitiesNamePayload = { 'EntitiesName': {} };
                    const metricsNamePayload = { 'MetricNames': {} };
                    const customPayload = { 'Custom': {} };
                    const payload = {
                        'source': undefined, 'sourceTotalLength': this.sourceTotalLength,
                        'parentFileName': undefined, 'category': this.deliveryTypeName, 'PIConfirmation': this.PIConfirmation,
                        'oldCorrelationId': undefined,
                        'languageFlag': this.language
                    };
                    const finalPayload = [];
                    let entityArrayLength = this.entityArray.length;
                    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        entityArrayLength = 1;
                    }
                    if (entityArrayLength >= 1) { // Payload for Entity
                        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (this.entityArray.length > 0) {
                                const entityName = this.apiRow.get('entityControl').value;
                                customPayload.Custom['startDate'] = this.apiRow.get('startDateControl').value.toLocaleDateString();
                                customPayload.Custom['endDate'] = this.apiRow.get('endDateControl').value.toLocaleDateString();
                                customPayload.Custom['ServiceType'] = this.apiRow.get('entityControl').value;
                                customPayload.Custom['Environment'] = this.env;
                                customPayload.Custom['RequestType'] = this.requestType;
                                payload.source = 'Custom';
                                payload.category = this.requestType;
                                this.selectedParentType = payload.source;
                            }
                        } else {
                            const entityName = this.apiRow.get('entityControl').value;
                            entityPayload.pad['startDate'] = this.apiRow.get('startDateControl').value.toLocaleDateString();
                            entityPayload.pad['endDate'] = this.apiRow.get('endDateControl').value.toLocaleDateString();
                            payload.source = 'Entity';
                            if (this.selectedParentType === undefined) {
                                this.selectedParentType = payload.source;
                            }
                            const entityObject = {};
                            entityName.forEach(entity => {
                                const selectedEntity = this.entityList.filter(x => x.name === entity);
                                entityObject[entity] = selectedEntity[0].subDeliverable;
                            });
                            entityPayload.pad['Entities'] = entityObject;
                            entityPayload.pad['method'] = undefined;
                            if (this.deliveryTypeName !== undefined && this.deliveryTypeName !== null) {
                                let deliveryName;
                                if (this.deliveryTypeName === 'Global') {
                                    deliveryName = 'Devops';
                                } else {
                                    deliveryName = this.deliveryTypeName;
                                }
                                entityPayload.pad['method'] = deliveryName.toUpperCase();
                            }
                            const entityNamesAsString = entityName.join(',');
                            entitiesNamePayload['EntitiesName'] = entityNamesAsString;
                        }
                    }
                    if (this.metricsArray.length >= 1) { // Payload for Metrics
                        let metricsName = this.metricRow.get('metricsControl').value;
                        metricsPayload.metrics['startDate'] = this.metricRow.get('metricStartDateControl').value.toLocaleDateString();
                        metricsPayload.metrics['endDate'] = this.metricRow.get('metricEndDateControl').value.toLocaleDateString();
                        metricsPayload.metrics['Granularity'] = this.iterationTypeUId;
                        //   metricsPayload.metrics['interval'] = this.metricRow.get('metricsIntervalControl').value;
                        payload.source = 'Metric';
                        if (this.selectedParentType === undefined) {
                            this.selectedParentType = payload.source;
                        }
                        if (Array.isArray(metricsName) === false) {
                            metricsName = metricsName.toArray();
                        }
                        metricsPayload.metrics['metrics'] = metricsName;
                        const metricsNames = [];
                        metricsName.forEach(metric => {
                            const selectedMetric = this.filteredMetricData.filter(x => x.Code === metric);
                            metricsNames.push(selectedMetric[0].Name);
                        });
                        const metricNamesAsString = metricsNames.join(',');
                        metricsNamePayload['MetricNames'] = metricNamesAsString;
                    }
                    if (this.files.length > 0) { // Payload for File
                        payload.source = 'File';
                        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (entityArrayLength >= 1) {
                                payload.source = 'File';
                                payload.category = this.requestType;
                            }
                        }
                        if (this.selectedParentType === undefined) {
                            this.selectedParentType = payload.source;
                        }
                        filePayload.fileUpload['filepath'] = this.files;
                        if (this.FileUploadFlag === true) {
                            payload.oldCorrelationId = localStorage.getItem('oldCorrelationID');
                        }
                    }
                    if (this.sourceTotalLength !== 1) { // If more than 1 source is uploaded
                        /* if (uploadType === 'parent') { // If more than 1 source is uploaded but continue with parent file is selected
                          if (this.selectedParentType === 'Entity') {
                            instMLPayload = {'InstaMl': {}};
                            metricsPayload = {'metrics': {}};
                            filePayload = {'fileUpload': {}};
                            payload.source = 'Entity';
                            const entityObject = {};
                            const selectedEntity = this.entityList.filter(x => x.name === this.selectedParentFile[0]);
                            entityObject[this.selectedParentFile[0]] = selectedEntity[0].subDeliverable;
                            entityPayload.pad['Entities'] = entityObject;
                            payload.parentFileName = this.selectedParentFile[0] + '.Entity';
                          }
                          if (this.selectedParentType === 'Metric') {
                            entityPayload = {'pad': {}};
                            instMLPayload = {'InstaMl': {}};
                            filePayload = {'fileUpload': {}};
                            payload.source = 'Metric';
                            metricsPayload.metrics['metrics'] = this.metricsArray;
                            payload.parentFileName = 'Metric.Metric';
                          }
                          if (this.selectedParentType === 'file') {
                            entityPayload = {'pad': {}};
                            metricsPayload = {'metrics': {}};
                            instMLPayload = {'InstaMl': {}};
                            payload.source = 'file';
                            this.files = this.files.filter(x => x.name === this.selectedParentFile[0]);
                            filePayload.fileUpload['filepath'] = this.files;
                            payload.parentFileName = this.selectedParentFile[0];
                          }
                          payload.source = this.selectedParentType;
                          payload.parentFileName = undefined;
                          payload.sourceTotalLength = 1;
                        }
                        if (uploadType === 'multipleFiles') { */ // If more than 1 source is uploaded but continue with multiple file is selected
                        if (this.selectedParentType === 'file') {
                            payload.parentFileName = this.selectedParentFile[0];
                        } else if (this.selectedParentType === 'Metric') {
                            payload.parentFileName = 'Metric.Metric';
                        } else if (this.selectedParentType === 'Custom') {
                            payload.parentFileName = this.selectedParentFile[0];
                        } else {
                            payload.parentFileName = this.selectedParentFile[0] + '.Entity';
                        }
                        //payload.source = this.selectedParentType;
                        // }
                        finalPayload.push(payload);
                        finalPayload.push(entityPayload);
                        finalPayload.push(instMLPayload);
                        finalPayload.push(metricsPayload);
                        finalPayload.push(filePayload);
                        finalPayload.push(entitiesNamePayload);
                        finalPayload.push(metricsNamePayload);
                        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            finalPayload.push(customPayload);
                        }
                        this.outputData(finalPayload);
                    } else { // If 1 source is uploaded
                        payload.parentFileName = undefined;
                        payload.source = this.selectedParentType;
                        payload.sourceTotalLength = 1;
                        finalPayload.push(payload);
                        finalPayload.push(entityPayload);
                        finalPayload.push(instMLPayload);
                        finalPayload.push(metricsPayload);
                        finalPayload.push(filePayload);
                        finalPayload.push(entitiesNamePayload);
                        finalPayload.push(metricsNamePayload);
                        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            finalPayload.push(customPayload);
                        }
                        this.outputData(finalPayload);
                    }
                }
            }
            else if (this.sourceTotalLength === 0 && this.FileUploadFlag === false) {
                const finalPayload = [];
                const startDate = this.apiRow.get('startDateControl').value;
                const endDate = this.apiRow.get('endDateControl').value;
                if (this.coreUtilsService.isNil(startDate) || this.coreUtilsService.isNil(endDate)) {
                    this.ns.error('Please select Start Date & End Date');
                } else {
                    this.entityStartDate = this.apiRow.get('startDateControl').value.toLocaleDateString();
                    this.entityEndDate = this.apiRow.get('endDateControl').value.toLocaleDateString();
                    const entityPayload = { 'pad': {} };
                    const instMLPayload = { 'InstaMl': {} };
                    const metricsPayload = { 'metrics': {} };
                    const filePayload = { 'fileUpload': {} };
                    const entitiesNamePayload = { 'EntitiesName': {} };
                    const metricsNamePayload = { 'MetricNames': {} };
                    const payload = {
                        'source': undefined, 'sourceTotalLength': this.sourceTotalLength,
                        'parentFileName': undefined, 'category': this.deliveryTypeName, 'PIConfirmation': this.PIConfirmation,
                        'entityStartDate': this.entityStartDate, 'entityEndDate': this.entityEndDate,
                        'languageFlag': this.language
                    };
                    payload.source = 'Custom';
                    payload.sourceTotalLength = 1;

                    finalPayload.push(payload);
                    finalPayload.push(entityPayload);
                    finalPayload.push(instMLPayload);
                    finalPayload.push(metricsPayload);
                    finalPayload.push(filePayload);
                    finalPayload.push(entitiesNamePayload);
                    finalPayload.push(metricsNamePayload);
                    this.outputData(finalPayload);
                }
            }
            else {
                this.ns.error('Kindly upload atleast one source of data.');
            }
        } else {
            if (this.datasetname === undefined) {
                this.ns.error('Please select the Dataset name');
            } else {
                const entityPayload = { 'pad': {} };
                const instMLPayload = { 'InstaMl': {} };
                const metricsPayload = { 'metrics': {} };
                const filePayload = { 'fileUpload': {} };
                const entitiesNamePayload = { 'EntitiesName': {} };
                const metricsNamePayload = { 'MetricNames': {} };
                const payload = {
                    'source': 'DataSet', 'sourceTotalLength': this.sourceTotalLength,
                    'parentFileName': undefined, 'category': this.deliveryTypeName, 'PIConfirmation': this.PIConfirmation,
                    'oldCorrelationId': undefined,
                    'languageFlag': this.language,
                    'DatasetUId': this.datasetname
                };
                const finalPayload = [];
                finalPayload.push(payload);
                finalPayload.push(entityPayload);
                finalPayload.push(instMLPayload);
                finalPayload.push(metricsPayload);
                finalPayload.push(filePayload);
                finalPayload.push(entitiesNamePayload);
                finalPayload.push(metricsNamePayload);
                this.outputData(finalPayload);
            }
        }
    }

    /* ValidateEntityDate() {
      const startDate = this.apiRow.get('startDateControl').value;
      const endDate = this.apiRow.get('endDateControl').value;
      if ((startDate > endDate) && startDate !== '' && endDate !== '') {
        this.apiRow.get('endDateControl').setErrors({ 'incorrect': true });
        throwError('Start date cannot be after the end date');
        this.ns.error('Start date cannot be after the end date');
      } else {
        this.apiRow.get('endDateControl').setErrors(null);
        this.apiRow.get('startDateControl').setErrors(null);
      }
    }
   
    ValidateMetricsDate() {
      const startDate = this.metricRow.get('metricStartDateControl').value;
      const endDate = this.metricRow.get('metricEndDateControl').value;
      if ((startDate > endDate) && startDate !== '' && endDate !== '') {
        this.metricRow.get('metricEndDateControl').setErrors({ 'incorrect': true });
        throwError('Start date cannot be after the end date');
        this.ns.error('Start date cannot be after the end date');
      } else {
        this.metricRow.get('metricEndDateControl').setErrors(null);
        this.metricRow.get('metricStartDateControl').setErrors(null);
      }
    } */

    onChangeOFEntity() {
        const entityValue = this.apiRow.get('entityControl').value;
        this.entityselection = true;
        let entityValueLength = entityValue.length;
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            entityValueLength = 1;
        }
        const sourceLength = this.files.length + entityValueLength + this.metricCount;
        if (sourceLength <= 5) {
            if (this.apiRow.get('startDateControl').status === 'VALID' &&
                this.apiRow.get('endDateControl').status === 'VALID') {
                this.entityArray = this.apiRow.get('entityControl').value;
                let entityArrayLength = this.entityArray.length;
                if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    if (this.removalEntityFlag) {
                        entityArrayLength = 0;
                        this.removalEntityFlag = false;
                    } else {
                        entityArrayLength = 1;
                    }
                }
                this.sourceTotalLength = this.files.length + entityArrayLength + this.metricCount;
            }
        } else {
            this.ns.error('Maximum 5 sources of data allowed.');
            //  if (this.env !== 'FDS' && (this.requestType !== 'AM' || this.requestType !== 'IO')) {
            this.apiRow.get('entityControl').setValue(this.entityArray);
            // }
        }
    }

    onChangeOFMetrics() {
        const metricValue = this.metricRow.get('metricsControl').value;
        if (this.metricsArray.length >= 1) {
            this.metricCount = 1;
        } else {
            this.metricCount = 0;
        }
        const sourceLength = this.files.length + this.entityArray.length + this.metricCount;
        if (sourceLength <= 5) {
            if (this.metricRow.get('metricStartDateControl').status === 'VALID' &&
                this.metricRow.get('metricEndDateControl').status === 'VALID') {
                if (metricValue.length <= 5) {
                    this.metricsArray = this.metricRow.get('metricsControl').value;
                    this.metricsText = this.metricsArray.join(',');
                    if (this.metricsArray.length >= 1) {
                        this.metricCount = 1;
                    } else {
                        this.metricCount = 0;
                    }
                    this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
                } else {
                    this.ns.error('Maximum 5 metrics selection is allowed.');
                    this.metricRow.get('metricsControl').setValue(this.metricsArray);
                }
            }
        } else {
            this.ns.error('Maximum 5 sources of data allowed.');
            this.metricRow.get('metricsControl').setValue(this.metricsArray);
        }
    }

    onClose() {
        this.modalRef.hide();
    }

    getFileDetails(e) {
        if (this.agileFlag === true) {
            this.uploadFilesCount = e.target.files.length;
        } else {
            this.uploadFilesCount += e.target.files.length;
        }
        let validFileExtensionFlag = true;
        let validFileNameFlag = true;
        let validFileSize = true;
        const resourceCount = e.target.files.length;
        if (this.sourceTotalLength < 5 && resourceCount <= (5 - this.sourceTotalLength)) {
            const files = e.target.files;
            let fileSize = 0;
            for (let i = 0; i < e.target.files.length; i++) {
                const fileName = files[i].name;
                const dots = fileName.split('.');
                const fileType = '.' + dots[dots.length - 1];
                if (!fileName) {
                    validFileNameFlag = false;
                    break;
                }
                if (this.allowedExtensions.indexOf(fileType) !== -1) {
                    fileSize = fileSize + e.target.files[i].size;
                    const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
                    validFileNameFlag = true;
                    validFileExtensionFlag = true;
                    if (index < 0) {
                        if (this.agileFlag === true) {
                            this.files = [];
                            this.files.push(e.target.files[0]);
                        } else {
                            this.files.push(e.target.files[i]);
                        }
                        this.uploadFilesCount = this.files.length;
                        // this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
                        let entityArrayLength = this.entityArray.length;
                        if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (this.removalEntityFlag) {
                                entityArrayLength = 0;
                            } else {
                                if (this.entityselection) {
                                    entityArrayLength = 1;
                                } else {
                                    entityArrayLength = 0;
                                }
                            }
                            this.sourceTotalLength = this.files.length + entityArrayLength + this.metricCount;
                        } else {
                            this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
                        }
                    }
                    /* if (e.target.files[i].size <= 136356582) {
                      const index =  this.files.findIndex(x =>  (x.name === e.target.files[i].name ));
                      validFileNameFlag = true;
                      validFileExtensionFlag = true;
                      validFileSize = true;
                      if (index < 0) {
                        this.files.push(e.target.files[i]);
                        this.uploadFilesCount = this.files.length;
                        this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
                      }
                    } else {
                      validFileSize = false;
                    } */
                } else {
                    validFileExtensionFlag = false;
                    break;
                }
            }
            if (fileSize <= 136356582) {
                validFileSize = true;
            } else {
                validFileSize = false;
                this.files = [];
                this.uploadFilesCount = this.files.length;
                this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
            }
            if (validFileNameFlag === false) {
                this.ns.error('Kindly upload a file with valid name.');
            }
            if (validFileExtensionFlag === false) {
                this.ns.error('Kindly upload .xlsx or .csv file.');
            }
            if (validFileSize === false) {
                this.ns.error('Kindly upload file of size less than 130MB.');
            }
        } else {
            this.uploadFilesCount = this.files.length;
            this.ns.error('Maximum 5 sources of data allowed.');
        }
    }

    allowDrop(event) {
        event.preventDefault();
    }

    onDrop(event) {
        this.sourceTotalLength = event.dataTransfer.files.length;
        event.preventDefault();

        for (let i = 0; i < event.dataTransfer.files.length; i++) {
            this.files.push(event.dataTransfer.files[i]);
        }

        //this.dialog.close(this.files);
    }

    public removeFile(fileAtIndex: number, fileName: string, fileInput) {
        this.files.splice(fileAtIndex, 1);
        const index = this.selectedParentFile.findIndex(x => (x === fileName));
        if (index >= 0) {
            this.selectedParentFile = [];
        }
        this.uploadFilesCount = this.files.length;
        /* if ( this.uploadFilesCount === 1) {
          this.apiRow.controls.apiSelect.setValue('parent');
        } */
        this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
        this.fileInput.nativeElement.value = '';
    }

    removeEntity(index: number, entityName: string) {
        if ((this.env !== 'PAM' || this.env !== 'FDS') && this.requestType !== 'AM' && this.requestType !== 'IO') {
            this.entityArray = this.entityArray.filter(e => e !== entityName);
        }
        let entityArrayLength = this.entityArray.length;
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            entityArrayLength = 0;
            this.removalEntityFlag = true;
            this.entityArray = [];
            this.apiRow.get('entityControl').setValue(this.entityArray);
        }
        this.sourceTotalLength = this.files.length + entityArrayLength + this.metricCount;
        this.apiRow.get('entityControl').setValue(this.entityArray);
    }

    removeMetrics() {
        this.metricsArray = [];
        this.metricCount = 0;
        this.metricsText = '';
        this.sourceTotalLength = this.files.length + this.entityArray.length + this.metricCount;
        this.metricRow.get('metricsControl').setValue(this.metricsArray);
    }

    public setParentFile(selectedParentName, type) {
        this.selectedParentFile[0] = selectedParentName;
        this.selectedParentType = type;
    }

    /**
     * Upload multiple files
     */
    private submitMultipeFiles() {
        if (!this.selectedParentFile.length) {
            this.ns.error('Please select parent file.');
        } else {
            const multipleFiles = this.files; // store all files
            multipleFiles['parentFileName'] = this.selectedParentFile[0].name;
            this.outputData(multipleFiles);
        }
    }

    /**
     * Upload parent file
     */
    private submitParentFile() {
        if (!this.selectedParentFile.length) {
            this.ns.error('Please select parent file.');
        } else {
            this.selectedParentFile['parentFileName'] = this.selectedParentFile[0].name;
            this.outputData(this.selectedParentFile);
        }
    }
    onChangeOfMetricIntervalOrIteration() {
        // this.selectedMetricTimeInterval = this.metricRow.get('metricsIntervalControl').value;
        this.selectedMetricIterationName = this.metricRow.get('metricsIterationControl').value;
        const iterationTypeUIdList = this.filteredMetricData.filter(x => x.IterationName === this.selectedMetricIterationName);
        this.iterationTypeUId = iterationTypeUIdList[0].IterationTypeUId;
        this.metricsList = this.filteredMetricData.filter(x => x.IterationName === this.selectedMetricIterationName);
        if (this.metricsList.length === 0) {
            this.ns.error('Kindly select different iteration.');
        }
    }

    onPIConfirmation(optionValue) {
        this.PIConfirmation = optionValue;
        if (this.PIConfirmation) {
            this.ns.success('Personal Identification Information is available in the data provided for analysis. Data will be encrypted and stored by the platform.')
        }
    }

    getEntityDetails(deliveryName) {
        this.problemStatementService.getDynamicEntity(this.clientUID,
            this.deliveryConstructUID, this.userId).subscribe(
                data => {
                    // this..loadingEnded();
                    this.entityLoader = 'apiDone';
                    let entitiesArray;
                    Object.entries(data).forEach(
                        ([key, value]) => {
                            entitiesArray = value;
                            entitiesArray.forEach(entity => {
                                const obj = { 'name': entity, 'subDeliverable': key };
                                this.entityList.push(obj);
                            });
                        });

                    // Filtering entity data on the basis of deliveryTypeName
                    this.entityList = this.entityList.filter(x => {
                        const deliveryTypes = x.subDeliverable.split('/');
                        if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
                            if (deliveryName === 'PPM' && x.name.indexOf('Iteration') > -1) {
                                return false;
                            } else {
                                return x;
                            }
                        }
                    });

                    if (this.entityList.length === 0) {
                        this.apiRow.get('startDateControl').disable();
                        this.apiRow.get('endDateControl').disable();
                    }
                },
                error => {
                    // this.apputilService.loadingEnded();
                    this.entityLoader = 'apiDone';
                    this.ns.error('Pheonix metrics api has no data.');
                });
    }

    filterMetricData() {
        if (this.metricData) {
            if (this.metricData.length > 0) {
                let deliveryType = this.deliveryTypeName;
                if (this.deliveryTypeName === 'Devops') {
                    deliveryType = 'Global';
                }
                this.filteredMetricData = this.metricData[0].MetricResultListData.filter(x => x.DeliveryTypeName === deliveryType);
                if (this.filteredMetricData && this.filteredMetricData.length > 0) {
                    // this.filteredMetricData = this.dialogConfig.data.filteredMetricData;
                    const metricTimeIntervalList = new Set(this.filteredMetricData.map(x => x.TimeInterval));
                    //  this.metricTimeInterval = Array.from(metricTimeIntervalList);
                    //  this.selectedMetricTimeInterval = this.metricTimeInterval[0];
                    const metricIterationNameList = new Set(this.filteredMetricData.map(x => x.IterationName));
                    this.metricIterationName = Array.from(metricIterationNameList);
                    this.selectedMetricIterationName = this.metricIterationName[0];
                    const iterationTypeUIdList = this.filteredMetricData.filter(x => x.IterationName === this.selectedMetricIterationName);
                    this.iterationTypeUId = iterationTypeUIdList[0].IterationTypeUId;
                    // Filtering metric data on the basis of time interval and iteration name
                    this.metricsList = this.filteredMetricData.filter(x => x.IterationName === this.metricIterationName[0]);
                    // getting unique values by metric code
                    const key = 'Code';
                    this.metricsList = Array.from(new Map(this.metricsList.map<[any, any]>(item => [item[key], item])).values());
                }
            }

        }

        if (this.filteredMetricData.length === 0) {
            //  this.metricRow.get('metricsIntervalControl').disable();
            this.metricRow.get('metricsIterationControl').disable();
            this.metricRow.get('metricStartDateControl').disable();
            this.metricRow.get('metricEndDateControl').disable();
        }

        this.metricRow.controls.metricsIterationControl.setValue(this.selectedMetricIterationName);
    }

    getMetricsData(clientUId, deliveryConstructUID, userId) {
        this.problemStatementService.getMetricsData(clientUId, deliveryConstructUID, userId).subscribe(
            data => {
                this.metricData = data;
                this.filterMetricData();
                this.metricsLoader = 'apiDone';
            },
            error => {
                this.ns.error('Pheonix metrics api has no data.');
                this.filterMetricData();
                this.metricsLoader = 'apiDone';
            });
    }

    ontoggleSwitchChanged(elementRef) {
        if (elementRef.checked === true) {
            this.FileUploadFlag = true;
        } else {
            this.FileUploadFlag = false;
            this.apiRow.get('startDateControl').enable();
            this.apiRow.get('endDateControl').enable();
            if (this.sourceTotalLength > 0) {
                this.sourceTotalLength = 0;
                this.files = [];
            }
        }
    }

    downlaodTemplate() {
        // if (flag === 'show') {
        //   this.modalRef = this._bsModalService.show(template);
        // } else {
        //  this.modalRef.hide();
        // this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
        // if (confirmationflag === true) { 
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    window.location.href = "assets/files/ModelTemplates/" + self.filenameToDownload;
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                window.location.href = "assets/files/ModelTemplates/" + self.filenameToDownload;
                //    }
            });
        }
        // }
    }

    downloadData() {
        if (localStorage.getItem('oldCorrelationID') != null) {
            this.correlationId = localStorage.getItem('oldCorrelationID');
        }
        //downloadData(template: TemplateRef<any>, flag) {
        // if (flag === 'show') {
        // this.modalRef = this._bsModalService.show(template);
        // } else {
        // this.modalRef.hide();
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    self.problemStatementService.getTemplateData(self.correlationId)
                        .subscribe(colData => {
                            if (colData) {
                                self.ns.success('Your Data will be downloaded shortly');
                                self.tableDataforDownload = colData['ColumnListDetails'] || [];
                                self._excelService.exportAsExcelFile(self.tableDataforDownload, 'TemplateDownloaded');
                            }
                        }, error => {
                            throwError(error);
                        });
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                self.problemStatementService.getTemplateData(self.correlationId)
                    .subscribe(colData => {
                        if (colData) {
                            self.ns.success('Your Data will be downloaded shortly');
                            self.tableDataforDownload = colData['ColumnListDetails'] || [];
                            self._excelService.exportAsPasswordProtectedExcelFile(self.tableDataforDownload, 'TemplateDownloaded').subscribe(response => {
                                self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                                let binaryData = [];
                                binaryData.push(response);
                                let downloadLink = document.createElement('a');
                                downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                                downloadLink.setAttribute('download', 'TemplateDownloaded' + '.zip');
                                document.body.appendChild(downloadLink);
                                downloadLink.click();
                            }, (error) => {
                                self.ns.error(error);
                            });
                        }
                    }, error => {
                        throwError(error);
                    });
            });
        }
        //  }
    }

    public typeOfDataToUpload(typeOfUpload: TypeOfUpload, datasource1: TypeOfData, datasource2?: TypeOfData): void {
        this.clearDataUpload();
        this.typeOfUpload = typeOfUpload;
        this.typeOfDataForSource1 = datasource1;
        this.typeOfDataForSource2 = datasource2;
        this.dataFabricOption = !(this.typeOfUpload === 'Single' && this.typeOfDataForSource1 === 'File');
        this.fileUploadView = this.typeOfDataForSource1 === 'File';
        this.entityMetricView = this.typeOfDataForSource2 === 'Metric' || this.typeOfDataForSource2 === 'Entity';

        if (this.typeOfDataForSource2 === 'Entity') {
            this.fileAndEntityDataToUpload();
        } else if (this.typeOfDataForSource2 === 'Metric') {
            this.fileAndMetricDataToUpload();
        } else if (this.typeOfDataForSource2 === 'Dataset') {
            this.datasetviewUpload();
        } else if (this.typeOfDataForSource2 === 'CustomData') {
            this.enableCustomDataView();
        }
        // else if (this.typeOfDataForSource2 === 'API'){
        //   this.apiviewupload();
        // }
    }

    public fileAndMetricDataToUpload(): void {
        this.entityLoader = null;
        this.datasetview = false;
        this.apiview = false;
        if (!this.metricData) {
            // if (this.metricData.length > 0) {
            this.metricsLoader = 'apiPending';
            if (( (this.env !== 'PAM' && this.env !== 'FDS') && (this.requestType !== 'AM' && this.requestType !== 'IO') )
            && (this.fromApp !== 'FDS' && this.fromApp !== 'PAM') ) {
                this.deliveryTypeName = this.deliveryTypeName;
                this.getMetricsData(this.clientUID, this.deliveryConstructUID, this.userId);
            } else {
                this.metricsLoader = 'apiDone';
            }
            // }
        }
        if (this.metricData) {
            if (this.metricData.length > 0) {
                this.metricsLoader = 'apiDone';
            }
        }
        // this.metricsLoader = 'apiDone';
    }

    selectedchoosetype(value) {
        this.datasetname = value;
    }

    selectedchoosesource(source) {
        this.choosetype = [];
        let sourcetype;
        if (source === 'API') {
            sourcetype = 'ExternalAPI';
        } else {
            sourcetype = source;
        }
        this.getdatasetdetails(sourcetype);
    }


    getdatasetdetails(sourcetype) {
        this.problemStatementService.showdatasetdropdown(this.clientUID,
            this.deliveryConstructUID, this.userId).subscribe(
                data => {
                    data.forEach(element => {
                        if (element["SourceName"] === sourcetype) {
                            const datasets = { 'name': element["DataSetName"], 'id': element["DataSetUId"] };
                            this.choosetype.push(datasets);
                        }
                    });

                });
    }


    public fileAndEntityDataToUpload(): void {
        this.entityLoader = 'apiDone';
        this.metricsLoader = null;
        this.datasetview = false;
        //this.apiview = false;
    }

    public datasetviewUpload(): void {
        this.entityLoader = null;
        this.metricsLoader = null;
        this.datasetview = true;
        // this.apiview\ = false;
    }

    // public apiviewupload() : void {
    //   this.entityLoader = null;
    //   this.metricsLoader = null;
    //   this.apiview = true;
    //   this.datasetview = false;
    // }

    public clearDataUpload(): void {
        this.files = [];
        this.entityArray = [];
        this.apiRow.get('entityControl').setValue(this.entityArray);
        this.metricsArray = [];
        this.metricCount = 0;
        this.metricRow.get('metricsControl').setValue(this.metricsArray);
        this.sourceTotalLength = 0;
        this.datasetview = false;
        this.customDataView = false;
        //  this.apiview = false;
    }

    public showDataFabricOptions() {
        this.clearDataUpload();
        this.dataFabricOption = true;
        this.entityMetricView = true;
        this.fileUploadView = false;
        this.datasetview = false;
        this.metricsLoader = null;
        //  this.apiview = false;
        this.typeOfDataForSource2 = 'Entity';
        if (this.entityLoader === null) { this.entityLoader = 'apiDone'; }
        // this.typeOfDataToUpload('Multiple', 'File', 'Metric');
    }

    downloadSampleTemplate() {

    }
    public enableCustomDataView(): void {
        this.customDataView = true;
        this.resetOptionView();
        this.addQueryView = true;
    }
    // Custom Data View methods
    public onOptionSelection(selectedOption: TypeOfOption) {
        this.resetOptionView();
        if (selectedOption === 'AddQuery') {
            this.addQueryView = true;
        } else if (selectedOption === 'API') {
            this.APIView = true;
            this.ApiConfig = {
                isDataSetNameRequired: false,
                isCategoryRequired: false,
                isIncrementalRequired: false
            };
        }
    }

    private resetOptionView() {
        this.addQueryView = this.APIView = false;
    }

    saveQuery(data) {
        const entityPayload = { 'pad': {} };
        const instMLPayload = { 'InstaMl': {} };
        const metricsPayload = { 'metrics': {} };
        const filePayload = { 'fileUpload': {} };
        const entitiesNamePayload = { 'EntitiesName': {} };
        const metricsNamePayload = { 'MetricNames': {} };
        const custom = { 'Custom': {} };
        const customData = { 'CustomDataPull': data.query, 'DateColumn': data.DateColumn }
        const payload = {
            'source': CustomDataTypes.Query,
            'sourceTotalLength': this.sourceTotalLength,
            'parentFileName': undefined,
            'category': this.deliveryTypeName,
            'PIConfirmation': data.PIConfirmation,
            'oldCorrelationId': undefined,
            'languageFlag': this.language,
            'DatasetUId': this.datasetname
        };

        const finalPayload = [];
        finalPayload.push(payload);
        finalPayload.push(entityPayload);
        finalPayload.push(instMLPayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(filePayload);
        finalPayload.push(entitiesNamePayload);
        finalPayload.push(metricsNamePayload);
        finalPayload.push(custom);
        finalPayload.push(customData);
        this.outputData(finalPayload);
    }

    saveAPI(filesData) {
        let AzureCredentials;
        if (filesData[0].clientid !== undefined && filesData[0].clientsecret !== undefined && filesData[0].resource !== undefined && filesData[0].grantype !== undefined) {
            AzureCredentials = {
                'client_id': filesData[0].clientid,
                'client_secret': filesData[0].clientsecret,
                'resource': filesData[0].resource,
                'grant_type': filesData[0].grantype
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
            "startDate": filesData[0].startdate,
            "endDate": filesData[0].enddate,
            "SourceType": CustomDataTypes.API,
            "Data": {
                "METHODTYPE": filesData[0].HttpMethod,
                "ApiUrl": filesData[0].Url,
                "KeyValues": filesData[0].Keyvalue,
                "BodyParam": filesData[0].Body,
                "fetchType": filesData[0].fetchType,
                "Authentication": {
                    "Type": filesData[0].authttype,
                    "Token": filesData[0].token,
                    "AzureUrl": filesData[0].tokenurl,
                    'AzureCredentials': AzureCredentials,
                    'UseIngrainAzureCredentials': filesData[0].UseIngrainAzureCredentials
                },
                "TargetNode": filesData[1].targetNode
            }
        };
        const entityPayload = { 'pad': {} };
        const instMLPayload = { 'InstaMl': {} };
        const metricsPayload = { 'metrics': {} };
        const filePayload = { 'fileUpload': {} };
        const entitiesNamePayload = { 'EntitiesName': {} };
        const metricsNamePayload = { 'MetricNames': {} };
        const custom = { 'Custom': {} };
        const customData = { 'CustomDataPull': apiFormData }
        const payload = {
            'source': CustomDataTypes.API,
            'sourceTotalLength': this.sourceTotalLength,
            'parentFileName': undefined,
            'category': this.deliveryTypeName,
            'PIConfirmation': filesData[0].PIConfirmation,
            'oldCorrelationId': undefined,
            'languageFlag': this.language,
            'DatasetUId': this.datasetname
        };

        const finalPayload = [];
        finalPayload.push(payload);
        finalPayload.push(entityPayload);
        finalPayload.push(instMLPayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(filePayload);
        finalPayload.push(entitiesNamePayload);
        finalPayload.push(metricsNamePayload);
        finalPayload.push(custom);
        finalPayload.push(customData);
        this.outputData(finalPayload);
    }


    verifyAdminUser() {
        this.appUtilsService.getRoleData().subscribe(userDetails => {
            const logedinUserRole = userDetails.accessRoleName;
            if (logedinUserRole === 'System Admin' || logedinUserRole === 'Client Admin' ||
                logedinUserRole === 'System Data Admin' || logedinUserRole === 'Solution Architect') {
                this.isAdminUser = true;
            }
        });
    }
    // End

    outputData(data){
        this.onClose();
        this.uploadedData.emit(data);
      }
}
