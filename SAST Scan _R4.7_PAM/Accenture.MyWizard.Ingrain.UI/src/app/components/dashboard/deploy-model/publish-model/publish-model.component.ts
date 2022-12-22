import { Component, OnInit, Inject, ElementRef, TemplateRef, Output, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, switchMap, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { DeployModelService } from 'src/app/_services/deploy-model.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { empty, Subject, Subscription } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { browserRefresh } from '../../../header/header.component';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import * as _ from 'lodash';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ApiService } from 'src/app/_services/api.service';
import { DatePipe } from '@angular/common';
import { CustomConfigComponent } from '../custom-config/custom-config.component';
import { serviceLevel, ServiceTypes } from '../../../../_enums/service-types.enum';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
    selector: 'app-publish-model',
    templateUrl: './publish-model.component.html',
    styleUrls: ['./publish-model.component.scss']
})

export class PublishModelComponent implements OnInit {
    serviceType = ServiceTypes;
    modalRef: BsModalRef | null;
    modalRefPublicParent: BsModalRef;
    modelRefPublicChildModel: BsModalRef;

    config = {
        backdrop: true,
        ignoreBackdropClick: true,
        class: 'deploymodle-confirmpopup'
    };

    modelName: String;
    datasource;
    useCase;
    correlationId;

    servicesToChoose = ['Link with web app', 'Create Web Service'];
    appsToDeploy = ['VDS', 'Guided Ticket Resolver(GTR)',
        'Automation Planner(AP)', 'Automation Journey(AJ)', 'Business Disruption Predictor(BDP)'];

    private apps = [
        { name: 'Virtual Data Scientist(VDS)', value: '1' },
        { name: 'Guided Ticket Resolver(GTR)', value: '2' },
        { name: 'Automation Planner(AP)', value: '3' },
        { name: 'Automation Journey(AJ)', value: '4' },
        { name: 'Business Disruption Predictor(BDP)', value: '5' }
    ];

    // variables to send in Post API
    saveselectedVersion = {};
    saveSelectedApps = [];
    deployappname: string;
    saveTypeOfDeployModel = {};
    //saveServiceType = {};
    saveTemplateName: String;
    newModelName: string;
    newCorrelationIdAfterCloned;
    modelsData;
    modelNames = [];
    trainedModels = [];
    modelAccurcy = [];
    selectedAccuracy: number;
    isPrivate = true;
    userID: string;
    isPopupClosed: boolean;
    webserviceLinkshow: boolean;
    trainingDataVolume: boolean;
    templates;
    subscription: Subscription;
    clientUId: string;
    deliveryConstructUID: string;
    public browserRefresh: boolean;
    templatesCategories;
    isLoading = true;
    isPublisModelTab = false;
    accessRole: any;
    accessRoleName: string;
    accessRoleTitle: string;
    isPublicModelAccess = true;
    publicModelAccessColor: string;
    problemType: string;
    isTimeSeries = false;
    modelVesionArray: any = [];
    modelId = [];
    modelFrequency = [];
    roundedAccuracy = [];
    selectedModelFrequency: string[] = [];
    deployedModelVersionName: string[];
    DeployedMVersion: string[] = [];
    wkAlertMsg: string;
    deployedFrequency: string[] = [];
    frequencyToReplace = [];
    showValidationMsg = false;
    disableAppToDeploy = false;
    checked = false;
    appname: string;
    checkAppsDeploye = {};
    errMsg: string;
    modelAccuracyObject = {};
    env;
    requestType;
    isInstaModel = false;
    isEnvironment = false;
    typeOfManagement = ['Program & Project Management',
        'Change Management'];
    deliveryTypeName;
    appsData: Array<any>;
    selectedAppId;
    readOnly;
    disableSave = false;
    isModelTemplate = false;
    isModelTemplateDataSource = false;
    openappwindow: string;
    categoryName;
    provideTrainingDataVolume: string;
    categoryText: string;

    currentIndex = 3;
    breadcrumbIndex = 0;
    isCascadingButton = false;
    IsFMModel = false;
    isOfflineSelected = false;
    isOnlineSelected = false;
    textChanged: Subject<string> = new Subject<string>();

    trainingFrequency: Array<any> = [
        // {
        //   name: 'Hourly', FrequencyVal: ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', '13', '14', '15',
        //     '16', '17', '18', '19', '20', '21', '22', '23']
        // },
        { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
        { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Fortnightly', FrequencyVal: ['1'] },
    ];

    trainingFrequencyValue: Array<any>;
    predictionFrequencyValue: Array<any>;

    reTrainingFrequency: Array<any> = [
        { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
        { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
        { name: 'Fortnightly', FrequencyVal: ['1'] },
    ];

    reTrainingFrequencyValue: Array<any>;

    retryAttemptValue = ['1', '2', '3'];

    selectedTFrequency = 'Frequency';
    selectedTFeqValues = 'Frequency Value';
    selectedTretryCount = 'Retry Attempt';
    tFrequency = '';

    selectedPFrequency = 'Frequency';
    selectedPFeqValues = 'Frequency Value';
    selectedPretryCount = 'Retry Attempt';
    pFrequency = '';

    selectedOfflineRetrain = false;
    selectedRFrequency = 'Frequency';
    selectedRFeqValues = 'Frequency Value';
    rFrequency = '';

    isPublicRadioSelected = false;
    ifSourceTypeIsFile = false;
    isTrainingFrequencyValueSelected = false;
    isValidDataVolume = false;
    isAllFrequencyValueSelected = false;
    offlineTrainingValueSelection = { 'trainingValue': false, 'trainingFrequencyValue': false, 'trainingRetryValue': false };
    offlinePredictionValueSelection = { 'predictionValue': false, 'predictionFrequencyValue': false, 'predictionRetryValue': false };
    isTrainingValid = false;
    isPredictionValid = false;
    ConfigStorageKey = 'CustomConfiguration';
    isDevTest = false;
    isValidRetraining = false;
    DisableModelTemplate: boolean = false;
    archiveModelDuration = [{ value: '6', viewValue: '6 months' }, { value: '9', viewValue: '9 months' },
    { value: '12', viewValue: '12 months' }, { value: '15', viewValue: '15 months' }, { value: '18', viewValue: '18 months' },
    { value: '21', viewValue: '21 months' }, { value: '24', viewValue: '24 months' }, { value: '27', viewValue: '27 months' },
    { value: '30', viewValue: '30 months' }, { value: '33', viewValue: '33 months' }, { value: '36', viewValue: '36 months' }];

    selectedArchivalMonth = '6';
    sourceName: string;
    isCustomConstraintsEnabled: boolean = false;

    @Output() isPreviousDisabled: EventEmitter<boolean> = new EventEmitter<boolean>();

    constructor(private _deploymodelService: DeployModelService, private _localStorageService: LocalStorageService,
        private _notificationService: NotificationService,
        private router: Router, private _dialogService: DialogService, private _problemStatementService: ProblemStatementService,
        private _appUtilsService: AppUtilsService, private customRouter: CustomRoutingService, private route: ActivatedRoute,
        private coreUtilService: CoreUtilsService, @Inject(ElementRef) private eleRef: ElementRef, private _modalService: BsModalService,
        private uts: UsageTrackingService, private api: ApiService, private datePipe: DatePipe,
        private elRef: ElementRef, private environmentService: EnvironmentService) { }


    ngOnInit() {
        // const modelForm = new FormGroup({
        //     targetData : new FormControl('', Validators.required)
        // })
        const ingrainURL = this.api.ingrainAPIURL.toLowerCase();

        if (ingrainURL.includes('stagept') || ingrainURL.includes('devtest') || ingrainURL.includes('devut')) {
            this.isDevTest = true;
        }

        if (this.environmentService.IsPADEnvironment()) {//Enable custom constraints option only for PAD environmant.
            this.isCustomConstraintsEnabled = true;
        }

        this.textChanged.pipe(debounceTime(1000), distinctUntilChanged())
            .subscribe(model => {
                this.isSpecialCharacter(model);
            });

        this.readOnly = sessionStorage.getItem('viewEditAccess');
        if (this.readOnly === 'true') {
            this.disableSave = true;
        }
        this.isLoading = false;
        this.accessRole = undefined;
        this.publicModelAccessColor = 'transparent-color';
        this.env = sessionStorage.getItem('Environment');
        this.requestType = sessionStorage.getItem('RequestType');

        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            this.isEnvironment = true;
        }
        this._appUtilsService.loadingStarted();
        this.correlationId = this._localStorageService.getCorrelationId();
        this.userID = this._appUtilsService.getCookies().UserId;
        this.uts.usageTracking('Deploy Model', 'Publish Model');
        this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {

            this.clientUId = paramData.clientUID;
            this.deliveryConstructUID = paramData.deliveryConstructUId;
            this.browserRefresh = browserRefresh;
        });
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            this.publicModelAccessColor = 'transparent-color';
            this.isPublicModelAccess = false;
        } else {
            this._appUtilsService.getRoleData().subscribe(roles => {
                this.accessRole = roles.accessPrivilegeCode;
                this.accessRoleName = roles.accessRoleName;
                if (!this.isInstaModel) {
                    this.accessRoleTitle = !this.coreUtilService.isNil(this.accessRole) && !this.coreUtilService.isNil(this.accessRoleName)
                        ? this.accessRoleName + ' | ' + this.accessRole : '';
                    this.publicModelAccessColor = !this.coreUtilService.isNil(this.accessRole) && !this.coreUtilService.isNil(this.accessRoleName)
                        && this.accessRole === 'RWD' ? 'transparent-color' : 'grey-color';
                    this.isPublicModelAccess = !this.coreUtilService.isNil(this.accessRole) && !this.coreUtilService.isNil(this.accessRoleName)
                        && this.accessRole === 'RWD' ? false : true;
                } else {
                    this.publicModelAccessColor = this.isInstaModel ? 'grey-color' : 'transparent-color';
                    this.isPublicModelAccess = this.isInstaModel ? true : false;
                }
            });
        }

        this.getDeployedModelByLoggedInUser();
        this.getNumberOfDeployedModel();

        this.route.queryParams
            .subscribe(params => {
                this._appUtilsService.loadingStarted();
                if (params.publisModelFlag) {
                    this.isPublisModelTab = params.publisModelFlag;
                }
                this._appUtilsService.loadingEnded();
            });

        this._deploymodelService.getIsModelDeployed(this.correlationId).subscribe(
            data => {
                if (data.DeployModels.length > 0 && !this.isPublisModelTab) {
                    if (data.DeployModels[0].Status === 'Deployed') {
                        this.coreUtilService.disableTabs(this.currentIndex, 1);
                        this.isLoading = false;
                        this._appUtilsService.loadingEnded();
                        this.router.navigate(['/dashboard/deploymodel/deployedmodel'],
                            {
                                queryParams: {
                                    'Accuracy': data.DeployModels.Accuracy, 'CorrelationId': data.DeployModels.CorrelationId,
                                    'DataSource': data.DataSource, 'IsPrivate': data.DeployModels.IsPrivate, 'LinkedApps': data.DeployModels.LinkedApps,
                                    'ModelName': data.DeployModels.ModelName, 'ModelURL': data.DeployModels.ModelURL,
                                    'WebServices': data.DeployModels.WebServices, 'BusinessProblem': data.BusinessProblem,
                                    'ModelVersion': data.DeployModels.ModelVersion, 'DeployedDate': data.DeployModels.DeployedDate,
                                    'Status': data.DeployModels.Status, 'InputSample': data.DeployModels.InputSample,
                                    'VDSLink': data.DeployModels.VDSLink, 'ClientUId': data.DeployModels.ClientUId,
                                    'DeliveryConstructUID': data.DeployModels.DeliveryConstructUID
                                }
                            });
                    }
                } else {
                    this._deploymodelService.getTrainedModel(this.correlationId).subscribe(
                        // tslint:disable: no-shadowed-variable
                        data => {
                            this.coreUtilService.disableTabs(this.currentIndex, this.breadcrumbIndex);
                            this.setAttributes(data);
                            this.isLoading = true;
                        });
                }
            }, error => {
                this._appUtilsService.loadingEnded();
                this._notificationService.error(`Error occurred: Due
        to some backend data process
         the relevant data could not be produced.
          Please try again while we troubleshoot the error`);
            });
        this.getPublicTemplates();

        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            this.appname = 'VDS';
        } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            this.appname = 'VDS(AIOPS)';
        } else {
            this.appname = 'VDS(SI)';
        }
        this.fetchDeployDetails();
    }

    setAttributes(data) {
        this.datasource = data.DataSource;
        if (this.datasource == "CustomDataAPI") {
            this._appUtilsService.loadingStarted();
            this.checkDisbleModelTemplate();
        }
        this.modelName = data.ModelName;
        this.useCase = data.BusinessProblems;
        this.problemType = data.ModelType;
        this.isInstaModel = data.InstaFlag;
        this.deliveryTypeName = data.Category;
        this.isModelTemplateDataSource = data.IsModelTemplateDataSource;
        this.isCascadingButton = data.IsCascadingButton;
        if (data['IsFmModel'] === true) {
            this.IsFMModel = true;
            this.coreUtilService.allTabs[1].status = 'disabled';
            this.coreUtilService.allTabs[2].status = 'disabled';
            this.isPreviousDisabled.emit(true);
        }

        if (this.problemType === 'TimeSeries') {
            this.isTimeSeries = true;
        } else {
            this.isTimeSeries = false;
        }
        this.modelsData = data;
        this.trainedModels = this.modelsData.TrainedModel;

        if (this.trainedModels) {
            this.getModels(this.trainedModels);
        }

        if (data.DeployedModelVersions) {
            this.deployedModelVersionName = data.DeployedModelVersions as string[];
            for (let i = 0; i < data.DeployedModelVersions.length; i++) {
                this.DeployedMVersion.push(data.DeployedModelVersions[i].ModelVersion);
            }
            for (const a in this.DeployedMVersion) {
                if (this.DeployedMVersion.length > 0) {
                    const existingFrequency = this.DeployedMVersion[a].split('_');
                    this.deployedFrequency.push(existingFrequency[1]);
                }
            }

        }
        this.saveSelectedApps = [];
        // if (this.isInstaModel) {
        //   // this.checkAppsDeploye['checked' + 0] = true;
        //   this.saveSelectedApps.push(this.appsToDeploy[0]);
        //  // this.disableAppToDeploy = true;
        // } else {
        //   this.saveSelectedApps = [];
        //   this.disableAppToDeploy = false;
        //   /*   for (const k of Object.keys(this.checkAppsDeploye)) {
        //       this.checkAppsDeploye[k] = false;
        //      }*/

        // }
        this._appUtilsService.loadingEnded();
        this.getPublicTemplates();

        if (this.datasource === null || this.datasource.indexOf('.') > -1) {
            this.ifSourceTypeIsFile = true;
        }
    }

    getModels(trainedModels) {
        for (let i = 0; i < trainedModels.length; i++) {
            this.modelNames.push(trainedModels[i].modelName);
            this.modelFrequency.push(trainedModels[i].Frequency);

            if (trainedModels[i].r2ScoreVal) {
                if (trainedModels[i].r2ScoreVal.error_rate || trainedModels[i].r2ScoreVal.error_rate === 0) {
                    this.modelAccurcy.push(trainedModels[i].r2ScoreVal.error_rate);
                    this.roundedAccuracy.push(Math.round(trainedModels[i].r2ScoreVal.error_rate * 100) / 100);
                    this.modelAccuracyObject[trainedModels[i].modelName] = trainedModels[i].r2ScoreVal.error_rate;
                }
            } else {
                this.modelAccurcy.push(trainedModels[i].Accuracy);
                this.roundedAccuracy.push(Math.round(trainedModels[i].Accuracy * 100) / 100);
                this.modelAccuracyObject[trainedModels[i].modelName] = trainedModels[i].Accuracy;
            }
            if (trainedModels[i]._id) {
                this.modelId.push(trainedModels[i]._id);
            }
        }
    }

    selectedVersionOption(value) {
        this.saveselectedVersion = {};
        let counter = 0;
        this.selectedModelFrequency = [];
        this.modelVesionArray = value;

        if (this.isTimeSeries) {
            for (let version = 0; version < this.modelVesionArray.length; version++) {
                for (let a = 0; a < this.modelNames.length; a++) {
                    if (this.modelVesionArray[version] === this.modelNames[a]) {

                        counter = version;
                        this.saveselectedVersion[counter] = {};
                        this.saveselectedVersion[counter]['ModelName'] = this.modelVesionArray[version];

                        if (this.modelAccurcy[a] === undefined) {
                            this.saveselectedVersion[counter]['ModelAccuracy'] = 0.0;
                        } else {
                            this.saveselectedVersion[counter]['ModelAccuracy'] = Math.round(this.modelAccurcy[a] * 100) / 100;
                        }
                        this.saveselectedVersion[counter]['ModelId'] = this.modelId[a];
                        this.saveselectedVersion[counter]['Frequency'] = this.modelFrequency[a];

                        this.selectedModelFrequency.push(this.modelFrequency[a]);
                    }
                }
                this.duplicateFrequency(this.selectedModelFrequency[version]);
            }
            if (this.errMsg) {
                this._notificationService.error(this.errMsg);
            }

        } else {
            this.saveselectedVersion = value;
            if (!this.isTimeSeries && this.problemType !== 'Regression') {
                this.selectedAccuracy = this.modelAccuracyObject[value];
            } else {
                for (let m = 0; m < this.modelNames.length; m++) {
                    for (let a = m; a < this.modelAccurcy.length; a++) {
                        if (value === this.modelNames[a]) {
                            this.selectedAccuracy = this.modelAccurcy[a];
                        }
                    }
                }
            }
            this.selectedAccuracy = Math.round(this.selectedAccuracy * 100) / 100;
        }
    }

    duplicateFrequency(item) {
        let counter = 0;
        for (const freq in this.modelVesionArray) {
            if (this.modelVesionArray[freq].indexOf(item) >= 0) {
                if (counter === 0) {
                    this.errMsg = '';
                } else {
                    this.errMsg = 'One ' + item + ' Frequency Already Selected, Please Select Other Frequency';
                    break;
                }
                counter = counter + 1;
            }
        }
    }

    rodioSelectedIsPublic(elementRef) {
        this.selectedOfflineRetrain = false;
        this.isOfflineSelected = false;
        this.isOnlineSelected = false;
        this.resetValuesOfflineOnline();
        if (!this.isPublicModelAccess) {
            if (elementRef.checked === true) {
                this.isPublicRadioSelected = true;
                if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    this.appname = 'VDS';
                } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    this.appname = 'VDS(AIOPS)';
                } else {
                    this.appname = 'Ingrain';
                }
                this.changeapp(this.appname);
                this.saveTypeOfDeployModel = {};
                this.isPrivate = false;
                this.isModelTemplate = false;
                this.trainingDataVolume = false;
                this.saveTypeOfDeployModel['Public'] = 'True';
                this.webserviceLinkshow = false;
                if (this.saveSelectedApps[0] === undefined) {
                    this.saveSelectedApps = [];
                    this.appsData.forEach(appvalue => {
                        if ((this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (appvalue.ApplicationName === 'VDS') {
                                this.saveSelectedApps[0] = 'VDS';
                            }
                        } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (appvalue.ApplicationName === 'VDS(AIOPS)') {
                                this.saveSelectedApps[0] = 'VDS(AIOPS)';
                            }
                        } else {
                            if (appvalue.ApplicationName === 'Ingrain') {
                                this.saveSelectedApps.push('Ingrain');
                            }
                        }
                    });
                }
                // this.modalRefPublicParent = this._modalService.show(publicParentTemplate, { class: 'deploymodle-publicparent' });
            }
        } else {
            return false;
        }
    }

    rodioSelectedIsPrivate(elementRef) {
        this.selectedOfflineRetrain = false;
        this.isOfflineSelected = false;
        this.isOnlineSelected = false;
        if (elementRef.checked === true) {
            this.resetValuesOfflineOnline();
            this.saveSelectedApps = [];
            if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                this.appname = 'VDS';
            } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                this.appname = 'VDS(AIOPS)';
            } else {
                this.appname = 'VDS(SI)';
            }
            this.changeapp(this.appname);
            this.isPublicRadioSelected = false;
            this.isPrivate = true;
            this.isModelTemplate = false;
            this.saveTypeOfDeployModel = {};
            this.webserviceLinkshow = false;
            this.trainingDataVolume = false;
            this.saveTypeOfDeployModel['Private'] = 'True';
            if (this.saveSelectedApps[0] === undefined) {
                this.appsData.forEach(appvalue => {
                    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        if (appvalue.ApplicationName === 'VDS') {
                            this.saveSelectedApps[0] = 'VDS';
                        }
                    } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        if (appvalue.ApplicationName === 'VDS(AIOPS)') {
                            this.saveSelectedApps[0] = 'VDS(AIOPS)';
                        }
                    } else {
                        this.saveSelectedApps[0] = 'VDS(SI)';
                    }
                });
            }
        }
    }

    Edit(confirmReplaceExistingRec: TemplateRef<any>) {
        this.modalRef = this._modalService.show(confirmReplaceExistingRec, this.config);
    }


    onValueChange(query: string) {
        this.textChanged.next(query);
    }


    isSpecialCharacter(input) {
        const regex = /^[0-9 ]+$/;
        //console.log(input);
        const isValid = regex.test(input);
        this.provideTrainingDataVolume = input;
        if (input && input.length > 0) {
            if (!isValid) {
                this._notificationService.error('Enter only numeric value');
                this.isValidDataVolume = false;
                return 0;
            } else {
                if (input < 4) {
                    this._notificationService.error('The min limit on data pull is 4 records. Please validate the value.');
                    this.isValidDataVolume = false;
                    return 1;
                }
                else if (input > 30000) {
                    this._notificationService.error('The max limit on data pull is 30000 records. Please validate the value.');
                    this.isValidDataVolume = false;
                } else {
                    this.isValidDataVolume = true;
                }
            }
        } else {
            this.isValidDataVolume = false;
        }
    }

    deployModel(confirmReplaceExistingRec: TemplateRef<any>) {
        if (this.DisableModelTemplate) {
            this.saveTypeOfDeployModel['ModelTemplate'] = 'False';
        }
        if (Object.keys(this.saveselectedVersion).length === 0) {
            this._notificationService.error('Please choose model version to deploy');
        }

        if (this.isPrivate === true) {
            if (this.saveSelectedApps[0] === undefined) {
                this.saveSelectedApps = [];
                this.saveTypeOfDeployModel = {};
                this.saveTypeOfDeployModel['Private'] = 'True';
                this.appsData.forEach(appvalue => {
                    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        if (appvalue.ApplicationName === 'VDS') {
                            this.saveSelectedApps[0] = 'VDS';
                        }
                    } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        if (appvalue.ApplicationName === 'VDS(AIOPS)') {
                            this.saveSelectedApps[0] = 'VDS(AIOPS)';
                        }
                    } else {
                        this.saveSelectedApps[0] = 'VDS(SI)';
                    }
                });
            }
        }

        if (this.saveSelectedApps.length === 0 && this.disableAppToDeploy === false) {
            this._notificationService.error('Please choose apps to deploy');
        }
        if (this.disableAppToDeploy === false) {
            if (Object.keys(this.saveselectedVersion).length !== 0 &&
                // Object.keys(this.saveServiceType).length !== 0 &&
                this.saveSelectedApps.length !== 0) {
                if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    this.saveTemplateName = 'AIops';
                } else {
                    this.saveTemplateName = this.categoryName;
                }
                if (this.isTimeSeries) {
                    this.frequencyToReplace = [];
                    if (this.saveTemplateName === '' && this.isModelTemplate === true) {
                        this._notificationService.error('Please choose category of model template');
                    }
                    else {
                        if (this.deployedModelVersionName) {
                            for (let version = 0; version < this.modelVesionArray.length; version++) {
                                for (const f in this.deployedFrequency) {
                                    if (this.selectedModelFrequency[version] === this.deployedFrequency[f]) {
                                        this.frequencyToReplace.push(this.deployedFrequency[f]);
                                        this.showValidationMsg = true;
                                    }
                                }
                            }
                            if (this.showValidationMsg) {
                                this.wkAlertMsg = 'There is an existing model with ' + this.frequencyToReplace + ' frequency do you want to replace it ?';
                                this.modalRef = this._modalService.show(confirmReplaceExistingRec, this.config);
                            } else {
                                if (Object.keys(this.saveselectedVersion).length !== 0) {
                                    if (this.errMsg !== '') {
                                        this._notificationService.error('Please select Model Version of different frequencies');
                                    } else {
                                        this.postDeployModelCall();
                                    }
                                }
                            }
                        }
                    }
                } else {
                    if (this.saveTemplateName === undefined && this.isModelTemplate === true) {
                        this._notificationService.error('Please choose category of model template');
                    } else {
                        if (Object.keys(this.saveselectedVersion).length !== 0) {
                            // this.provideTrainingDataVolume = null;
                            if (this.provideTrainingDataVolume === undefined && this.isModelTemplate === true) {
                                this._notificationService.error('Please provide training data volume');
                            } else {
                                this.postDeployModelCall();
                            }
                        }
                    }
                }
            }
        } else {
            // if (Object.keys(this.saveselectedVersion).length !== 0 && Object.keys(this.saveServiceType).length !== 0) {
            if (Object.keys(this.saveselectedVersion).length !== 0) {
                this.saveTemplateName = this.categoryName;
                if (this.isTimeSeries) {
                    if (this.saveTemplateName === undefined && this.isModelTemplate === true) {
                        this._notificationService.error('Please choose category of model template');
                    }
                    else {
                        this.frequencyToReplace = [];
                        if (this.deployedModelVersionName) {
                            for (let version = 0; version < this.modelVesionArray.length; version++) {
                                for (const f in this.deployedFrequency) {
                                    if (this.selectedModelFrequency[version] === this.deployedFrequency[f]) {
                                        this.frequencyToReplace.push(this.deployedFrequency[f]);
                                        this.showValidationMsg = true;
                                    }
                                }
                            }
                            if (this.showValidationMsg) {
                                this.wkAlertMsg = 'There is an existing model with ' + this.frequencyToReplace + ' frequency do you want to replace it ?';
                                this.modalRef = this._modalService.show(confirmReplaceExistingRec, this.config);
                            } else {
                                if (Object.keys(this.saveselectedVersion).length !== 0) {
                                    if (this.errMsg !== '') {
                                        this._notificationService.error('Please select Model Version of different frequencies');
                                    } else {
                                        this.postDeployModelCall();
                                    }
                                }
                            }
                        }
                    }
                } else {
                    if (this.saveTemplateName === undefined && this.isModelTemplate === true) {
                        this._notificationService.error('Please choose category of model template');
                    } else {
                        if (Object.keys(this.saveselectedVersion).length !== 0) {
                            //this.postDeployModelCall();
                        }
                    }
                }
            }
        }
    }

    // confirmation popup ok event
    confirm(): void {
        if (this.errMsg !== '') {
            this._notificationService.error('Please select Model Version of different frequencies');
        } else {
            this.postDeployModelCall();
        }
        this.modalRef.hide();
    }

    // confirmation popup cancle event
    decline(): void {
        for (const s in this.saveselectedVersion) {
            if (Object.keys(this.saveselectedVersion).length !== 0) {
                for (const d in this.frequencyToReplace) {
                    if (this.saveselectedVersion[s] === undefined) {
                        this.modelVesionArray = [];
                        // return this._notificationService.error('Please select model version to deploy');
                    } else {
                        if (this.saveselectedVersion[s].Frequency === this.frequencyToReplace[d]) {
                            delete this.saveselectedVersion[s];
                        }
                    }
                }
            }
        }
        if (Object.keys(this.saveselectedVersion).length === 0) {
            this.router.navigate(['/dashboard/deploymodel/publishmodel'],
                {
                    queryParams: {
                        'publisModelFlag': true
                    }
                });
        } else {
            this.postDeployModelCall();
        }
        this.modalRef.hide();
    }

    // call post api of deploy model this.saveServiceType, this.selectedAccuracy
    postDeployModelCall() {

        let retryKey = 'RetryCount';
        let trainingObj = {};
        let predictionObj = {};
        let retraining = {};

        if (this.selectedTFrequency !== 'Frequency') {
            trainingObj[this.tFrequency] = this.selectedTFeqValues;
            trainingObj[retryKey] = this.selectedTretryCount;
        }

        if (this.selectedPFrequency !== 'Frequency') {
            predictionObj[this.pFrequency] = this.selectedPFeqValues;
            predictionObj[retryKey] = this.selectedPretryCount;
        }

        if (this.selectedRFrequency !== 'Frequency') {
            retraining[this.rFrequency] = this.selectedRFeqValues;
        }

        if (this.isPrivate) {
            this.isOfflineSelected = false;
        }

        this.uts.usageTracking('Create Custom Model', 'Publish Model');
        this._appUtilsService.loadingStarted();
        this._deploymodelService.postDeployModels(this.correlationId, this.saveselectedVersion, this.saveSelectedApps,
            this.saveTypeOfDeployModel, this.selectedAccuracy, this.isPrivate, this.saveTemplateName,
            this.userID, this.modelName, this.problemType, this.frequencyToReplace, this.selectedAppId, this.isModelTemplate,
            this.provideTrainingDataVolume, this.isOfflineSelected, this.isOnlineSelected, this.selectedOfflineRetrain,
            trainingObj, predictionObj, retraining, this.selectedArchivalMonth).subscribe(data => {
                if (data) {
                    console.log(data);
                    if (this.isOfflineSelected == true || this.isOnlineSelected == true) {
                        const trainingCustomConfig = sessionStorage.getItem(`${ServiceTypes.Training}${this.ConfigStorageKey}`);
                        const predictionCustomConfig = sessionStorage.getItem(`${ServiceTypes.Prediction}${this.ConfigStorageKey}`);
                        const reTrainingCustomConfig = sessionStorage.getItem(`${ServiceTypes.ReTraining}${this.ConfigStorageKey}`);
                        if (trainingCustomConfig != null || predictionCustomConfig != null || reTrainingCustomConfig != null) {
                            this.saveCustomConfiguration();
                        }
                    }
                    this._appUtilsService.loadingEnded();
                    this._notificationService.success('Model Deployed Successfully');
                    this.coreUtilService.disableTabs(this.currentIndex, 1);
                    if (this.isTimeSeries) {
                        this.router.navigate(['/dashboard/deploymodel/deployedmodel'],
                            {
                                queryParams: {
                                    'TimeSeriesData': data.DeployModels, 'DataSource': data.DataSource, 'UseCase': data.BusinessProblem
                                }
                            });
                    } else {
                        this.router.navigate(['/dashboard/deploymodel/deployedmodel'],
                            {
                                queryParams: {
                                    'accuracy': data.DeployModels.Accuracy, 'CorrelationId': data.DeployModels.CorrelationId,
                                    'DataSource': data.DataSource, 'IsPrivate': data.DeployModels.IsPrivate, 'LinkedApps': data.DeployModels.LinkedApps,
                                    'ModelName': data.DeployModels.ModelName, 'ModelURL': data.DeployModels.ModelURL,
                                    'WebServices': data.DeployModels.WebServices, 'UseCase': data.BusinessProblem,
                                    'TrainedModelName': data.DeployModels.ModelVersion, 'DeployDate': data.DeployModels.DeployedDate,
                                    'Status': data.DeployModels.Status, 'pyloadForWebLink': data.DeployModels.InputSample,
                                    'MaxDataPull': data.DeployModels.MaxDataPull
                                }
                            });
                    }

                }
            }, error => {
                this._notificationService.error('something went wrong in PostDeployModel')
                this._appUtilsService.loadingEnded();
            });
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
                    this.router.navigate(['dashboard/deploymodel/publishmodel'], {
                        queryParams: { modelName: this.newModelName, correlationId: data }
                    });
                    this._notificationService.success('Cloned Successfully');
                }
            })
        );
        openTemplateAfterClosed.subscribe();
    }

    openWebServiceURLWIndow() {
        if (this.isPopupClosed) {
        } else {
            this.webserviceLinkshow = true;
        }
        this.isPopupClosed = false;
    }

    closeWebSerciveURLWindow() {
        this.webserviceLinkshow = false;
        this.isPopupClosed = true;
        return this.webserviceLinkshow;
    }

    onSelectedTemplate(elementRef, template) {
        if (elementRef.checked === true) {
            this._deploymodelService.setTemplateName(template);
        }
    }

    getPublicTemplates() {
        if (this.deliveryTypeName === 'release Management' || this.deliveryTypeName === 'AD') {
            this.categoryName = 'AD';
            this.categoryText = 'System Integration'
        } else if (this.deliveryTypeName === 'Agile Delivery' || this.deliveryTypeName === 'Agile') {
            this.categoryName = 'Agile';
            this.categoryText = 'Agile';
        } else if (this.deliveryTypeName === 'Devops') {
            this.categoryName = 'Devops';
            this.categoryText = 'Devops';
        } else if (this.deliveryTypeName === 'AIops' || this.deliveryTypeName === 'AM' || this.deliveryTypeName === 'IO') {
            this.categoryName = 'AIops';
            this.categoryText = 'AIops';
        } else if (this.deliveryTypeName === 'PPM') {
            this.categoryName = 'PPM';
            this.categoryText = 'PPM';
        } else {
            this.categoryName = 'Others';
            this.categoryText = 'Others';
        }
    }

    fetchDeployDetails() {
        this._problemStatementService.getDeployAppsDetails(this.correlationId, this.clientUId, this.deliveryConstructUID).subscribe(data => {
            this.appsData = data;
            if (this.env === 'PAM') {
                this.appname = this.appsData.find(model => model.ApplicationName === 'VDS')?.ApplicationName;
            }
            this.changeapp(this.appname);
            // console.log(this.appsData);
        });
    }

    fetchApp(data) {
        if (data === 'Success') {
            this.fetchDeployDetails();
        }
    }

    changeapp(appname) {

        if (this.openappwindow === 'close' || this.openappwindow === 'Success' || this.openappwindow === 'Error') {
            this.openappwindow = 'appchange';
        }
        this.saveSelectedApps = [];
        this.saveSelectedApps.push(appname);
        const appId = this.appsData.filter(model => model.ApplicationName === appname);
        this.selectedAppId = (appId.length > 0) ? appId[0].ApplicationId : "";
    }

    showaddnew() {
        this.openappwindow = 'edit';
    }

    closeapp(data) {
        this.openappwindow = data;
    }

    selectedCategories(value) {
        // this.categoryName = value;
        for (let a in this.templatesCategories) {
            if (this.templatesCategories[a].name === value) {
                this.categoryName = this.templatesCategories[a].value;
            }
        }
    }

    rodioSelectedIsModelTemplate(elementRef) {
        this.selectedOfflineRetrain = false;
        this.isOfflineSelected = false;
        this.isOnlineSelected = false;
        this.provideTrainingDataVolume = '';
        this.isValidDataVolume = false;
        this.resetValuesOfflineOnline();
        if (!this.isPublicModelAccess) {
            if (elementRef.checked === true) {
                if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    this.appname = 'VDS';
                } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                    this.appname = 'VDS(AIOPS)';
                } else {
                    this.appname = 'Ingrain';
                }
                this._notificationService.warning(`If this model is deployed as a Model Template, 
        models further created using this template will be archived after the archival duration.`)
                this.saveTypeOfDeployModel = {};
                this.isPrivate = false;
                this.isModelTemplate = true;
                if (this.isDevTest) {
                    this.trainingDataVolume = true;
                }
                this.isPublicRadioSelected = false;
                // this.targetData = '';
                this.saveTypeOfDeployModel['ModelTemplate'] = 'True';
                this.webserviceLinkshow = true;
                if (this.saveSelectedApps[0] === undefined) {
                    this.saveSelectedApps = [];
                    this.appsData.forEach(appvalue => {
                        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (appvalue.ApplicationName === 'VDS') {
                                this.saveSelectedApps[0] = 'VDS';
                            }
                        } else if ((this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                            if (appvalue.ApplicationName === 'VDS(AIOPS)') {
                                this.saveSelectedApps[0] = 'VDS(AIOPS)';
                            }
                        } else {
                            if (appvalue.ApplicationName === 'Ingrain') {
                                this.saveSelectedApps.push('Ingrain');
                            }
                        }
                    });
                }
                // this.modalRefPublicParent = this._modalService.show(publicParentTemplate, { class: 'deploymodle-publicparent' });
            }
        } else {
            return false;
        }
    }

    accessDenied() {
        this._notificationService.error('Access Denied');
    }

    rodioSelectedIsOffline(elementRef) {
        if (elementRef.checked === true) {
            this.resetValuesOfflineOnline();
            this.isOfflineSelected = true;
            this.isOnlineSelected = false;
            this.isModelTemplate = true;
        }
    }

    rodioSelectedIsOnline(elementRef) {
        if (elementRef.checked === true) {
            this.resetValuesOfflineOnline();
            this.isOfflineSelected = false;
            this.isOnlineSelected = true;
            this.isModelTemplate = true;
            this.selectedOfflineRetrain = true;
            this.isTrainingFrequencyValueSelected = false;
            this.isAllFrequencyValueSelected = false;
            this.isValidRetraining = false;
        }
    }

    selectedTrainingFrequency(value) {
        this.selectedTFrequency = value;
        this.tFrequency = value.name;
        this.offlineTrainingValueSelection.trainingValue = this.tFrequency ? true : false;
        this.trainingFrequencyValue = this.trainingFrequency.find(feq => feq.name == value.name).FrequencyVal;
        this.isTrainingValid = this.offlineTrainingValueSelection.trainingValue && this.offlineTrainingValueSelection.trainingFrequencyValue && this.offlineTrainingValueSelection.trainingRetryValue;
    }

    selectedTFeqValue(value) {
        this.selectedTFeqValues = value;
        this.offlineTrainingValueSelection.trainingFrequencyValue = this.selectedTFeqValues ? true : false;
        this.isTrainingValid = this.offlineTrainingValueSelection.trainingValue && this.offlineTrainingValueSelection.trainingFrequencyValue && this.offlineTrainingValueSelection.trainingRetryValue;
    }

    selectedTRetryAttempt(value) {
        this.selectedTretryCount = value;
        this.offlineTrainingValueSelection.trainingRetryValue = this.selectedTretryCount ? true : false;
        this.isTrainingValid = this.offlineTrainingValueSelection.trainingValue && this.offlineTrainingValueSelection.trainingFrequencyValue && this.offlineTrainingValueSelection.trainingRetryValue;
    }

    selectedPredictionFrequency(pValue) {
        this.selectedPFrequency = pValue;
        this.pFrequency = pValue.name;
        this.offlinePredictionValueSelection.predictionValue = this.pFrequency ? true : false;
        this.predictionFrequencyValue = this.trainingFrequency.find(feq => feq.name == pValue.name).FrequencyVal;
        this.isPredictionValid = this.offlinePredictionValueSelection.predictionValue && this.offlinePredictionValueSelection.predictionFrequencyValue && this.offlinePredictionValueSelection.predictionRetryValue;
    }

    selectedPFeqValue(pValue) {
        this.selectedPFeqValues = pValue;
        this.offlinePredictionValueSelection.predictionFrequencyValue = this.selectedPFeqValues ? true : false;
        this.isPredictionValid = this.offlinePredictionValueSelection.predictionValue && this.offlinePredictionValueSelection.predictionFrequencyValue && this.offlinePredictionValueSelection.predictionRetryValue;
    }

    selectedPRetryAttempt(value) {
        this.selectedPretryCount = value;
        this.offlinePredictionValueSelection.predictionRetryValue = this.selectedPretryCount ? true : false;
        this.isPredictionValid = this.offlinePredictionValueSelection.predictionValue && this.offlinePredictionValueSelection.predictionFrequencyValue && this.offlinePredictionValueSelection.predictionRetryValue;
    }

    checkboxSelectedIsOffline(elementRef) {
        if (elementRef.checked === true) {
            this.selectedOfflineRetrain = true;
        } else {
            this.selectedOfflineRetrain = false;
            this.resetValuesOfflineOnline();
        }
    }

    checkboxSelectedIsOfflineRetraining(elementRef) {
        if (elementRef.checked === true) {
            this.selectedOfflineRetrain = true;
            this.isValidRetraining = true;
        } else {
            this.selectedOfflineRetrain = false;
            this.isTrainingFrequencyValueSelected = false;
            this.isAllFrequencyValueSelected = false;
            this.isValidRetraining = false;
            this.selectedRFrequency = 'Frequency';
            this.selectedRFeqValues = 'Frequency Value'
            this.rFrequency = '';
            this.reTrainingFrequencyValue = [];
        }
    }

    selectedRetrainingFrequency(rValue) {
        this.selectedRFrequency = rValue;
        this.rFrequency = rValue.name;
        this.reTrainingFrequencyValue = this.reTrainingFrequency.find(feq => feq.name == rValue.name).FrequencyVal;
        this.isTrainingFrequencyValueSelected = true;
    }

    selectedRFeqValue(pValue) {
        this.selectedRFeqValues = pValue;
        this.isAllFrequencyValueSelected = this.selectedOfflineRetrain ? this.isTrainingFrequencyValueSelected && pValue : true;
    }

    resetValuesOfflineOnline() {
        this.offlineTrainingValueSelection = { 'trainingValue': false, 'trainingFrequencyValue': false, 'trainingRetryValue': false };
        this.offlinePredictionValueSelection = { 'predictionValue': false, 'predictionFrequencyValue': false, 'predictionRetryValue': false };
        this.selectedTFrequency = 'Frequency';
        this.selectedTFeqValues = 'Frequency Value';
        this.selectedTretryCount = 'Retry Attempt';

        this.selectedPFrequency = 'Frequency';
        this.selectedPFeqValues = 'Frequency Value';
        this.selectedPretryCount = 'Retry Attempt';

        this.selectedRFrequency = 'Frequency';
        this.selectedRFeqValues = 'Frequency Value';

        this.selectedOfflineRetrain = false;

        this.rFrequency = '';

        this.isTrainingFrequencyValueSelected = false;
        this.trainingFrequencyValue = [];
        this.predictionFrequencyValue = [];
        this.isAllFrequencyValueSelected = false;
        this.isTrainingValid = false;
        this.isPredictionValid = false;
    }

    selectArchivalDuration(archivalMonths) {
        this.selectedArchivalMonth = archivalMonths;
        console.log('test----', this.selectedArchivalMonth)
    }

    // code to show Compelition Certificate only for marketplace user  : start
    getDeployedModelByLoggedInUser() {
        //  debugger;
        // let data;
        let models: any;
        let totalRec: any;
        const deployedModelCountArr = [];
        let deployedModelCount: any;
        const today = new Date();
        const date = this.formatDate(today);
        this._problemStatementService.getModels(false, this.userID, null, date, this.deliveryConstructUID, this.clientUId).subscribe(
            data => {
                models = data.filter(
                    models => models.ClientUId === this.clientUId && models.DeliveryConstructUID === this.deliveryConstructUID && models.CreatedByUser === this.userID);
                // && (models.CreatedByUser === this.userID || models.ModifiedByUser === this.userID));
                totalRec = models.length;
                for (let a = 0; a < totalRec; a++) {
                    if (models[a]['Status'] === 'Deployed') {
                        deployedModelCountArr.push(models[a]['Status']);
                    }
                    if (models[a]['CorrelationId'] === this.correlationId) {
                        this.sourceName = models[a]['SourceName'];
                    }
                }
                if (deployedModelCountArr.length > 0) {
                    deployedModelCount = deployedModelCountArr.length;
                } else {
                    deployedModelCount = 0;
                }
                localStorage.setItem('DeployedModelCount', deployedModelCount);
            });
    }

    formatDate(date) {
        const priorDate = this.addMonths(date, -5);
        return this.datePipe.transform(priorDate, 'yyyy-MM-dd');
    }

    addMonths(date, months) {
        date.setMonth(date.getMonth() + months);
        return date;
    }

    getNumberOfDeployedModel() {
        let showCertificatePopup;
        this._deploymodelService.getTrainedModel(this.correlationId).subscribe(
            data => {
                if (data.DeployedModelVersions) {
                    if (data.DeployedModelVersions.length > 0) {
                        showCertificatePopup = 'False';
                    } else {
                        showCertificatePopup = 'True';
                    }
                    localStorage.setItem('FlagtoShowCertificateModel', showCertificatePopup);
                }
            }, error => {
            });
    }
    // code to show Compelition Certificate only for marketplace user : end

    // code to show custom configuration Region - starts.
    showCustomConfiguration(selectedService) {
        this.sourceName = (this.sourceName == "pad") ? "Entity" : this.sourceName;
        this._modalService.show(CustomConfigComponent, { class: 'modal-dialog modal-lg modal-dialog-centered', backdrop: 'static', initialState: { title: 'Custom Configuration', dataSource: this.sourceName, selectedService: selectedService, serviceLevel: 'SSAI' } }).content.customConfig.subscribe(customConfig => {
            const configKey = `${selectedService}${this.ConfigStorageKey}`;
            if (customConfig.length > 0) {
                sessionStorage.setItem(configKey, JSON.stringify(customConfig));
            } else {
                sessionStorage.removeItem(configKey);
            }
        });
    }
    // code to show custom configuration Region - ends.

    // code to save custom configuration Region - starts.
    saveCustomConfiguration() {
        const payload = {
            "ApplicationID": this.selectedAppId,
            "CorrelationID": this.correlationId,
            "userId": this.userID,
            "clientUID": this.clientUId,
            "deliveryUID": this.deliveryConstructUID,
            "ModelName": this.modelName,
            "ModelVersion": this.saveselectedVersion,
            "ModelType": this.problemType,
            "DataSource": this.datasource
        }

        this._deploymodelService.saveCustomConfiguration(payload, serviceLevel.SSAI).subscribe(response => {
            this._notificationService.success('Custom Configuration saved successfully.');
            this._deploymodelService.clearCustomConfigStorage();
        }, error => {
            this._notificationService.error('something Went Wrong while saving Custom Configurations.');
            this._deploymodelService.clearCustomConfigStorage();
        });

    }
    // code to save custom configuration Region - ends.

    // Method to disable model template radio button , when custom Data API model created with 'Same as Ingrain Token' by providing different environment API Url.
    checkDisbleModelTemplate() {
        this.api.get('GetCustomSourceDetails', { 'correlationid': this.correlationId, 'CustomSourceType': CustomDataTypes.API }).subscribe(response => {
            if (!this.coreUtilService.isEmptyObject(response)) {
                if (response.Data.Authentication.Type == 'Token') {
                    this.DisableModelTemplate = true;
                }
            }
            this._appUtilsService.loadingEnded();
        }, error => {
            this._appUtilsService.loadingEnded();
            this._notificationService.error('something went wrong while fetching API Response');
        });
    }
    // merthod region Ends.
}
