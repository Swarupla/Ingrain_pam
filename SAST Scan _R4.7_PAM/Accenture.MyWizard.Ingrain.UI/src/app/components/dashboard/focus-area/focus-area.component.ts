import { Component, OnInit, ViewChild, ElementRef, Renderer2, OnDestroy, AfterContentChecked } from '@angular/core';
import { ProblemStatementService } from '../../../_services/problem-statement.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { UploadFileComponent } from 'src/app/components/upload-file/upload-file.component';
import { UploadApiComponent } from 'src/app/components/upload-api/upload-api.component';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { FilesMappingModalComponent } from 'src/app/components/files-mapping-modal/files-mapping-modal.component';
import { Router, ActivatedRoute } from '@angular/router';
import { tap } from 'rxjs/operators';
import { FileUploadProgressBarComponent } from 'src/app/components/file-upload-progress-bar/file-upload-progress-bar.component';
import { Subscription } from 'rxjs';
import { AppUtilsService } from '../../../_services/app-utils.service';
import { CoreUtilsService } from '../../../_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { TruncatePublicModelnamePipe } from '../../../_pipes/truncate-public-modelname.pipe';
import { browserRefresh } from '../../header/header.component';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ModelsTrainingStatusPopupComponent } from '../../header/models-training-status-popup/models-training-status-popup.component';
import { LetsgetstartedPopupComponent } from '../../letsgetstarted-popup/letsgetstarted-popup.component';
import { StatusPopupComponent } from '../../status-popup/status-popup.component';
import { HostListener } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ClientDeliveryStructureService } from 'src/app/_services/client-delivery-structure.service';
import { ApiService } from 'src/app/_services/api.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { BsModalService } from 'ngx-bootstrap/modal';

@Component({
    selector: 'app-focus-area',
    templateUrl: './focus-area.component.html',
    styleUrls: ['./focus-area.component.scss'],
    providers: [TruncatePublicModelnamePipe]
})
export class FocusAreaComponent implements OnInit, OnDestroy, AfterContentChecked {

    @ViewChild('searchInput', { static: false }) searchInput: any;

    public browserRefresh: boolean;
    displayModelGroupContent = false;
    templatesCategories;
    templates;
    templateModelNames = undefined;
    selectedIndex;

    searchText;
    userId: any;
    date;
    models;
    Myfiles: any;

    modelName: any;
    correlationId: any;
    subscription: Subscription;
    paramData: any;
    totalRec: number;

    clientUId: string;
    deliveryConstructUID: string;
    templateClass: string;
    myModelClass: string;
    linkTemplateClass: string;
    linkMyModelClass: string;
    language: string;
    truncatedModelNames = [];
    truncatedmodelcorrelationid = {};
    metricData;
    filteredMetricData;
    deliveryTypeName;
    trackingFeature;
    trackingSubFeature;

    typeOfManagement = ['Program & Project Management',
        'Change Management',
        'Release and Deployment Management'];

    predictTypes = ['Predict release stability post go live',
        'Predict critical violations and code vulnerabilities',
        'Predict build process time, build success and number of check-ins'];

    treeMenu = [];
    level = 1;
    prebuilt: string;

    tileClicked = false;
    view;
    category = 'release Management';

    categoryTypes = [{}];

    modelTrainingStatus = 'success';

    modelTrainingData = [];
    isAdmin: boolean;
    showBellIcon = true;

    allNlpServices;
    env;
    requestType;

    filteredNlpServices;
    fromApp;
    entityAPI = true;
    metricsAPI = true;
    templateCategoryView = false;
    toggelSideBar = false;
    pinUnpinIconsBar = false;
    toggelIconsBar = false;
    prebuiltModels = undefined;
    nlpServices;
    toggle: boolean = true;
    fromSourceSession;
    instanceType;

    isTestDrive: boolean = true;
    constructor(private route: ActivatedRoute,
        private problemStatementService: ProblemStatementService, private coreUtilsService: CoreUtilsService,
        private apputilService: AppUtilsService, private ls: LocalStorageService,
        private dialogService: DialogService, private router: Router, private truncateString: TruncatePublicModelnamePipe,
        private el: ElementRef, private ns: NotificationService, private uts: UsageTrackingService, private datePipe: DatePipe,
        private clientDelStructService: ClientDeliveryStructureService, private api: ApiService, private environmentService :EnvironmentService,
        private _modalService : BsModalService) {
    }

    ngOnInit() {
        //to check for non test drive environment.
        const ingrainURL = this.api.ingrainAPIURL.toLowerCase();
        if (ingrainURL.includes('stagept') || ingrainURL.includes('devtest') || ingrainURL.includes('devut')) {
            this.isTestDrive = false;
        }
        //Non test drive check region Ends.
        this.instanceType = sessionStorage.getItem('Instance');
        this.userId = this.apputilService.getCookies().UserId;
        if (sessionStorage.getItem('Environment') !== 'PAM') {
          this.getLanguageInfo();
        }
        if (localStorage.getItem('fromSource') === 'FDS' || localStorage.getItem('fromSource') === 'PAM') {
            this.fromSourceSession = localStorage.getItem('fromSource');
        }

        localStorage.setItem('featureMapping', 'False');
        this.entityAPI = true;
        this.metricsAPI = true;

        this.env = sessionStorage.getItem('Environment');
        this.requestType = sessionStorage.getItem('RequestType');
        if (sessionStorage.getItem('fromSource') !== null) {
            this.fromApp = sessionStorage.getItem('fromSource').toUpperCase();
        }
        if (((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) || (this.fromSourceSession === 'fds' || this.fromSourceSession === 'PAM') || (this.instanceType === 'PAM')) {
            this.categoryTypes = [
                { 'name': 'AIops', 'value': 'AIops', 'imgPath': './assets/images/aiops.svg' },
                { 'name': 'Others', 'value': 'Others', 'imgPath': './assets/images/others.svg' },
            ];
        } else if (this.env == null && (this.environmentService.isTestDriveEnvironment() == true)) {
            this.categoryTypes = [{ 'name': 'System Integration', 'value': 'release Management', 'imgPath': './assets/images/si.svg' },
            { 'name': 'Agile', 'value': 'Agile', 'imgPath': './assets/images/agile.svg' },
            { 'name': 'Devops', 'value': 'Devops', 'imgPath': './assets/images/devops.svg' },
            { 'name': 'Others', 'value': 'Others', 'imgPath': './assets/images/others.svg' },
            { 'name': 'PPM', 'value': 'PPM', 'imgPath': './assets/images/RIAD.svg' },
            { 'name': 'AIops', 'value': 'AIops', 'imgPath': './assets/images/aiops.svg' }
            ];
        } else {
            this.categoryTypes = [{ 'name': 'System Integration', 'value': 'release Management', 'imgPath': './assets/images/si.svg' },
            { 'name': 'Agile', 'value': 'Agile', 'imgPath': './assets/images/agile.svg' },
            { 'name': 'Devops', 'value': 'Devops', 'imgPath': './assets/images/devops.svg' },
            { 'name': 'Others', 'value': 'Others', 'imgPath': './assets/images/others.svg' },
            { 'name': 'PPM', 'value': 'PPM', 'imgPath': './assets/images/RIAD.svg' },
            ];
        }
        this.ls.setLocalStorageData('modelName', '');
        this.ls.setLocalStorageData('modelCategory', '');
        this.templateClass = 'tab-pane fade show active';
        this.myModelClass = 'tab-pane fade';
        this.linkTemplateClass = 'nav-link active show';
        this.linkMyModelClass = 'nav-link ';
        this.userId = this.apputilService.getCookies().UserId;

        /**Marketplace redirection code : start */
        if (this.router.url.indexOf('isMPuser') > 1) {
            this.onTileClicked('nlpservices');
        }
        if (localStorage.getItem('marketPlaceRedirected') === 'True') {
            localStorage.removeItem('marketPlaceRedirected');
        }
        /**Marketplace redirection code : end */

        this.subscription = this.apputilService.getParamData().subscribe(paramData => {
            this.paramData = paramData;
            this.clientUId = paramData.clientUID;
            this.deliveryConstructUID = paramData.deliveryConstructUId;
            this.browserRefresh = browserRefresh;
        });
        this.route.queryParams
            .subscribe(params => {
                if (!this.coreUtilsService.isNil(params.clientUId) && !this.coreUtilsService.isNil(params.deliveryConstructUID)) {
                    this.clientUId = params.clientUId;
                    this.deliveryConstructUID = params.deliveryConstructUID;
                    this.displayModelGroupContent = false;
                    this.templateClass = 'tab-pane fade';
                    this.myModelClass = 'tab-pane fade show active';
                    this.linkTemplateClass = 'nav-link';
                    this.linkMyModelClass = 'nav-link active show';

                }
            });

        this.getTempate();
        this.onClickCategory(this.category, 0);

        this.getDefaultModelsTrainingStatus(false);
        localStorage.removeItem('CustomFlag');

        this.apputilService.getRoleData().subscribe(userDetails => {
            if (userDetails.accessRoleName === 'System Admin' || userDetails.accessRoleName === 'Client Admin'
                || userDetails.accessRoleName === 'System Data Admin' || userDetails.accessRoleName === 'Solution Architect') {
                this.isAdmin = true;
            } else {
                this.isAdmin = false;
            }
        });
    }
    ngOnDestroy() {
        this.subscription.unsubscribe();
    }
    ngAfterContentChecked() {
        // this.filterMetricData();
    }

    getLanguageInfo() {
        const clientUId = sessionStorage.getItem('clientID');
        const deliveryConstructUId = sessionStorage.getItem('dcID');
        this.apputilService.loadingStarted();
        this.subscription = this.clientDelStructService.getLanguage(clientUId,
            deliveryConstructUId, this.userId).subscribe(response => {
                this.apputilService.loadingEnded();
                this.language = response['language'];
                sessionStorage.setItem('Language', this.language);
            },
                error => {
                    this.apputilService.loadingEnded();
                    if (error.status === 401) {
                        this.ns.error('token has expired So redirecting to login page');
                    } else {
                        sessionStorage.setItem('Language', 'english');
                    }
                });
    }

    redirecttoutilities() {
        this.problemStatementService.isPredefinedTemplate = 'False';
        this.uts.usageTracking('Problem Statement', 'My Utilities');
        this.router.navigate(['/dashboard/problemstatement/utilities']);
    }

    @HostListener('window:beforeunload', ['$event'])
    onWindowClose(event) {
        if (sessionStorage.getItem('progress') === 'Inprocess') {
            event.preventDefault();
            event.returnValue = true;
        }
    }

    getTempate() {
        const me = this;
        this.problemStatementService.getPublicTemplatess(true, this.userId, 'release Management',
            null, this.deliveryConstructUID, this.clientUId).subscribe(
                data => {
                    me.templates = data;
                    me.templatesCategories = JSON.parse(this.templates.Categories).Category;
                }
            );
        const today = new Date();
        this.date = this.formatDate(today); // '2019-04-30';

        this.apputilService.loadingStarted();
        this.problemStatementService.getModels(false, this.userId, null, this.date, this.deliveryConstructUID, this.clientUId).subscribe(
            data => {
                me.models = data.filter(
                    models => models.ClientUId === this.clientUId && models.DeliveryConstructUID === this.deliveryConstructUID);
                me.totalRec = me.models.length;
                this.apputilService.loadingEnded();
            }
        );
    }
    setCorrelationIdinLocalStorage(correlationId) {
        this.ls.setLocalStorageData('correlationId', correlationId);
    }

    setModelNameinLocalStorage(modelName) {
        this.ls.setLocalStorageData('modelName', modelName);
    }

    onClickCategory(templatesCategory, index: number) {
        this.level++;
        this.treeMenu.push(templatesCategory);
        this.truncatedModelNames = [];
        this.category = templatesCategory;
        const value = templatesCategory;
        this.displayModelGroupContent = true;
        this.selectedIndex = index;
        if (value) {
            this.apputilService.loadingStarted();
            this.problemStatementService.getPublicTemplatess(true, this.userId, value,
                null, this.deliveryConstructUID, this.clientUId).subscribe(
                    data => {
                        this.apputilService.loadingEnded();
                        this.templates = data;
                        this.templateModelNames = this.templates.publicTemplates;
                        // if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
                        //     let FM_ModelName = '[Release Success Predictor, AIops]';
                        //     if (this.templateModelNames) {
                        //         this.templateModelNames = this.templateModelNames.filter(models => models.Category === this.requestType || models.ModelName === FM_ModelName);
                        //     }
                        // }
                        if (this.templateModelNames !== null) {
                            this.templateModelNames.forEach(i => {
                                this.truncatedModelNames.push(this.truncateString.transform(i.ModelName));
                                const templateName = i.ModelName.slice(1, -1);
                                i.TemplateName = templateName.split(',');
                                if (i.LinkedApp !== null) {
                                    const LinkedApps = i.LinkedApp.replace('[', '');
                                    i.LinkedApps = LinkedApps.replace(']', '');
                                }
                                this.truncatedmodelcorrelationid[this.truncateString.transform(i.ModelName)]
                                    = [(i.CorrelationId), templatesCategory];
                            });
                            this.prebuiltModels = this.templateModelNames; console.log('PreBuiltModels-', this.templateModelNames)
                        } else {
                            this.prebuiltModels = this.templateModelNames;
                        }
                    },
                    error => {
                        this.apputilService.loadingEnded();
                    }
                );
        }
        this.trackingFeature = templatesCategory;
        if (templatesCategory === 'release Management' || templatesCategory === 'AD') {
            this.deliveryTypeName = 'AD';
        } else if (templatesCategory === 'Agile Delivery' || templatesCategory === 'Agile') {
            this.deliveryTypeName = 'Agile';
        } else if (templatesCategory === 'Devops') {
            this.deliveryTypeName = 'Devops';
        } else {
            this.deliveryTypeName = templatesCategory;
        }
        return false;
        // this.filterMetricData();
    }

    onSelectedModel(correlationid, tempcategory, name, CustomFlag, IsCascadeModelTemplate?) {
        sessionStorage.removeItem('viewEditAccess');
        const correlationId = correlationid; // this.truncatedmodelcorrelationid[name][0];
        localStorage.setItem('oldCorrelationID', correlationId);
        if (CustomFlag === 'true') {
            localStorage.setItem('CustomFlag', 'True');
        }
        const tempCategory = tempcategory; // this.truncatedmodelcorrelationid[name][1];
        const displayUploadandDataSourceBlock = true;
        const tempModelName = name;
        this.ls.setLocalStorageData('correlationId', correlationId);
        this.ls.setLocalStorageData('modelName', tempModelName);
        this.ls.setLocalStorageData('modelCategory', tempCategory);
        sessionStorage.setItem('IsCascadeModelTemplate', IsCascadeModelTemplate);
        if (IsCascadeModelTemplate === true) {
            this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
                {
                    queryParams: {
                        'modelCategory': tempCategory,
                        'displayUploadandDataSourceBlock': displayUploadandDataSourceBlock,
                        'isCascadeModelTemplate': true,
                        modelName: tempModelName
                    }
                });
        } else {
            this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
                {
                    queryParams: {
                        'modelCategory': tempCategory,
                        'displayUploadandDataSourceBlock': displayUploadandDataSourceBlock,
                        modelName: tempModelName
                    }
                });
        }
    }

    selectMyModels() {
        // sessionStorage.setItem('ifPreDefinedTemplate', 'True');
        this.problemStatementService.isPredefinedTemplate = 'False';
        this.uts.usageTracking('Problem Statement', 'My Models');
        this.router.navigate(['/dashboard/problemstatement/templates']);
        /* this.renderer.setElementClass(this.myModelsTable.nativeElement, 'd-none', false);
        this.renderer.setElementClass(this.landingPageContent.nativeElement, 'd-none', true);
        this.renderer.setElementStyle(this.myModelsTable.nativeElement, 'opacity', '1'); */
    }

    openModalForApi() {
        if (localStorage.getItem('oldCorrelationID') !== 'null') {
            localStorage.setItem('oldCorrelationID', 'null');
        }
        this.uts.usageTracking('Create New Model', 'Browse');
        if (this.coreUtilsService.isNil(this.paramData.clientUID) || this.coreUtilsService.isNil(this.paramData.deliveryConstructUId)) {
            this.ns.success('Kindly select a client or delivery structures');
        } else {
            this.openModalDialog();
        }
    }

    openModalDialog() {
        this.apputilService.loadingImmediateEnded();
        this._modalService.show(UploadApiComponent, {
            class: 'modal-dialog modal-xl', backdrop: 'static', initialState: {
                filteredMetricData: this.filteredMetricData, deliveryTypeName: this.deliveryTypeName,
                clientUID: this.paramData.clientUID, deliveryConstructUID: this.deliveryConstructUID, userId: this.userId,
                fromApp: this.fromApp
            }
        }).content.uploadedData.subscribe(filesData => {
            this.openModalNameDialog(filesData);
        });
    }

    openModalPopUp() {

        const fileUploadAfterClosed = this.dialogService.open(UploadFileComponent, {}).afterClosed
            .pipe(
                tap(filesData => filesData ? this.openModalNameDialog(filesData) : '')
            );

        fileUploadAfterClosed.subscribe();
    }

    openModalNameDialog(filesData) {
        this.Myfiles = filesData;
        this.showModelEnterDialog(filesData);
    }

    /* Show File Upload Process Dialog */
    openFileProgressDialog(filesData, modelName) {
        this.modelName = modelName;
        const totalSourceCount = filesData[0].sourceTotalLength;
        if (totalSourceCount > 1) {
            const openFileProgressAfterClosed = this.dialogService.open(FileUploadProgressBarComponent,
                { data: { filesData: filesData, modelName: modelName } }).afterClosed.pipe(
                    tap(data => data ? this.openModalMultipleDialog(data.body, modelName, filesData, data.dataForMapping) : '')
                );
            openFileProgressAfterClosed.subscribe();
        } else {
            const openFileProgressAfterClosed = this.dialogService.open(FileUploadProgressBarComponent,
                { data: { filesData: filesData, modelName: modelName } }).afterClosed.pipe(
                    tap(data => data ? this.navigateToUseCaseDefinition(data.body[0]) : '')
                );
            openFileProgressAfterClosed.subscribe();
        }
    }

    /* Multiple File Mapping Dialog */
    openModalMultipleDialog(filesData, modelName, fileUploadData, fileGeneralData) {
        this.modelName = modelName;
        if (filesData.Flag === 'flag3' || filesData.Flag === 'flag4') {
            const openFileMappingTemplateAfterClosed = this.dialogService.open(FilesMappingModalComponent,
                { data: { filesData: filesData, fileUploadData: fileUploadData, fileGeneralData: fileGeneralData } }).afterClosed.pipe(
                    tap(data => data ? this.navigateToUseCaseDefinition(data[0]) : '')
                );
            openFileMappingTemplateAfterClosed.subscribe();
        } else {
            if (filesData.CorrelationId !== undefined) {
                this.navigateToUseCaseDefinition(filesData.CorrelationId);
            } else {
                if (filesData[0] !== undefined) {
                    this.navigateToUseCaseDefinition(filesData[0]);
                }
            }
        }
    }

    /* Enter Model Name Dialog */
    showModelEnterDialog(filesData) {
        const openTemplateAfterClosed =
            this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name' } }).afterClosed.pipe(
                tap(modelName => modelName ? this.openFileProgressDialog(filesData, modelName) : '')
            );

        openTemplateAfterClosed.subscribe();
    }

    navigateToUseCaseDefinition(correlationId) {
        this.correlationId = correlationId;
        this.ls.setLocalStorageData('correlationId', correlationId);
        this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
            {
                queryParams: {
                    'modelCategory': '',
                    'displayUploadandDataSourceBlock': false,
                    'modelName': this.modelName
                }
            });
    }

    deleteModelByCorrelationId(correlationId: string) {
        if (!this.coreUtilsService.isNil(correlationId)) {
            this.apputilService.loadingStarted();
            this.problemStatementService.deleteModelByCorrelationId(correlationId).subscribe(
                data => {
                    this.ngOnInit();
                    this.apputilService.loadingEnded();
                }
            );
        }
    }

    formatDate(date) {
        const priorDate = this.addMonths(date, -5);
        return this.datePipe.transform(priorDate, 'yyyy-MM-dd');
    }
    addMonths(date, months) {
        date.setMonth(date.getMonth() + months);
        return date;
    }

    onClickOfManagement(management, index) {
        this.level++;
        this.treeMenu.push(management);
        this.trackingSubFeature = management;
        this.uts.usageTracking(this.trackingFeature, this.trackingSubFeature);
    }

    createNewModel() {
        this.level++;
        this.prebuilt = 'createNew';
        this.treeMenu.push('HAVE SIMILAR DATA');
        this.uts.usageTracking(this.trackingSubFeature, 'I have data that is related to this problem');
    }

    showPrebuiltTemplates() {
        this.level++;
        this.prebuilt = 'prebuilt';
        this.treeMenu.push('PRE-BUILT TEMPLATES');
        this.uts.usageTracking(this.trackingSubFeature, 'I like to choose from the pre-built templates');
    }
    onSelectOfNoData() {
        this.level++;
        this.prebuilt = 'noData';
        // commented
        // tslint:disable-next-line: quotemark
        this.treeMenu.push("DOESN'T HAVE DATA");
        this.uts.usageTracking(this.trackingSubFeature, 'I don’t have relevant data');
    }
    onClickOfPredictType(predictType) {
        this.level++;
        this.treeMenu.push(predictType);
    }
    previousLevel() {
        this.level--;
        this.treeMenu.pop();
    }
    changeLevel(levelNumber) {
        this.level = levelNumber;
        this.treeMenu = this.treeMenu.slice(0, (levelNumber - 1));
    }

    onTileClicked(view) {
        this.toggelSideBar = false;
        this.view = view;
        this.tileClicked = true;
        this.problemStatementService.isPredefinedTemplate = 'False';
        this.templateCategoryView = true;
        if (this.view === 'prebuilt') {
            // Bug 713507 Ingrain_StageTest_R2.1 -[Prebuilt template]- Able to make changes and save the prebuilt Templates
            // Fix:- added isPredefinedTemplate member in problemstatementserivice and same is used use case defination 
            this.problemStatementService.isPredefinedTemplate = 'True';
            if (((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) || (this.fromSourceSession === 'fds' || this.fromSourceSession === 'PAM')) {
                this.category = 'AIops';
            }
            this.onClickCategory(this.category, 0);
        }
        if (this.view === 'createNewModel') {
            if (((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) || (this.fromSourceSession === 'fds' || this.fromSourceSession === 'PAM')) {
                this.category = 'AIops';
            }
        }
        if (this.view === 'nlpservices') {
            this.category = 'Others';
            this.getAllNlpServices();
        }
    }
    getAllNlpServices() {
        this.apputilService.loadingStarted();
        this.problemStatementService.getAllAIServices().subscribe(data => {
            this.apputilService.loadingEnded();
            this.allNlpServices = data;
            const fileteredData = data.filter(el => el.Category === this.category && el.Active === true);
            this.filteredNlpServices = fileteredData;
            this.nlpServices = this.filteredNlpServices;
        });
    }
    redirectToNlpServices(serviceId) {
        // this.router.navigate(['/dashboard/reusable-NLP-services']);
    }
    onCategoryClick(categoryValue) {
        if (this.view === 'nlpservices') {
            //  this.category = 'Others';
            this.category = categoryValue;
            this.filteredNlpServices = this.allNlpServices.filter(el => el.Category === this.category);
            this.nlpServices = this.filteredNlpServices;
        } else {
            this.category = categoryValue;
        }
        if (this.category === 'release Management' || this.category === 'AD') {
            this.deliveryTypeName = 'AD';
        } else if (this.category === 'Agile Delivery' || this.category === 'Agile') {
            this.deliveryTypeName = 'Agile';
        } else if (this.category === 'Devops') {
            this.deliveryTypeName = 'Devops';
        } else {
            this.deliveryTypeName = categoryValue;
        }
        // this.filterMetricData();

        return false;
    }

    getDefaultModelsTrainingStatus(openPopup) {
        this.apputilService.loadingStarted();
        this.problemStatementService.getDefaultModelsTrainingStatus(this.clientUId, this.deliveryConstructUID, this.userId)
            .subscribe(data => {
                // if (data.length === 0) {
                //     this.showWhiteBellIcon = true;
                // } else 
                if (typeof data === 'object') {
                    this.showBellIcon = true;
                    // this.showWhiteBellIcon = false;
                    this.modelTrainingData = data;
                    if (this.modelTrainingData !== null) {
                        this.modelTrainingData.map(data => {
                            if (data.Status !== 'C') {
                                this.modelTrainingStatus = 'error';
                                return;
                            }
                        });
                        this.apputilService.loadingEnded();
                        if (openPopup) {
                            this.openModelsTrainingStatusPopup();
                        }
                    }
                } else {
                    this.apputilService.loadingEnded();
                    this.showBellIcon = false;
                    // this.showWhiteBellIcon = false;
                    this.modelTrainingStatus = 'error';
                }
            }, error => {
                this.apputilService.loadingEnded();
                this.ns.error('Error occurred: Due to some backend data process the relevant data could not be produced. Please try again while we troubleshoot the error.');
            });
    }

    openModelsTrainingStatusPopup() {
        const openModelsTrainingStatusAfterClosed = this.dialogService.open(ModelsTrainingStatusPopupComponent,
            { data: this.modelTrainingData }).afterClosed.pipe(
                tap(data => '')
            );
        openModelsTrainingStatusAfterClosed.subscribe();
    }

    backToDefaultView() {
        this.templateCategoryView = false;
        this.category = 'release Management';
    }
    categorySideBarToggling() {
        this.toggelSideBar = !this.toggelSideBar;
    }
    iconsPinUnpin() {
        this.pinUnpinIconsBar = !this.pinUnpinIconsBar;
    }
    toggleIconsBar() {
        this.toggelIconsBar = !this.toggelIconsBar;
    }

    searchModel(searchText) {
        if (this.view === 'prebuilt') {
            this.prebuiltModels = this.templateModelNames;
            if (!this.coreUtilsService.isNil(searchText)) {
                this.prebuiltModels = this.templateModelNames.filter((item) => {
                    return (!this.coreUtilsService.isNil(item.TemplateName[0]) && item.TemplateName[0].toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                });
            }
        }
        if (this.view === 'nlpservices') {
            this.nlpServices = this.filteredNlpServices;
            if (!this.coreUtilsService.isNil(searchText)) {
                this.nlpServices = this.filteredNlpServices.filter((item) => {
                    return (!this.coreUtilsService.isNil(item.ServiceName) && item.ServiceName.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                });
            }
        }
    }

    closeSearchBox() {
        this.searchInput = '';
        if (this.view === 'prebuilt') {
            this.prebuiltModels = this.templateModelNames;
        }
        if (this.view === 'nlpservices') {
            this.nlpServices = this.filteredNlpServices;
        }
    }
}
