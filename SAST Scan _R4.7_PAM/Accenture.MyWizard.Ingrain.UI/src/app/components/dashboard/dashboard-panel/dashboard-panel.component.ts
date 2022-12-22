import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { Routes, RouterModule, Router, ActivatedRoute, NavigationStart } from '@angular/router';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { ShowDataComponent } from 'src/app/components/dashboard/data-engineering/preprocess-data/show-data/show-data.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { PstUseCaseDefinitionComponent } from 'src/app/components/dashboard/problem-statement/pst-use-case-definition/pst-use-case-definition.component';
import { DataCleanupComponent } from 'src/app/components/dashboard/data-engineering/data-cleanup/data-cleanup.component';
import { PreprocessDataComponent } from 'src/app/components/dashboard/data-engineering/preprocess-data/preprocess-data.component';
import { FeatureSelectionComponent } from 'src/app/components/dashboard/model-engineering/feature-selection-model/feature-selection.component';
import { RecommendedAIComponent } from 'src/app/components/dashboard/model-engineering/recommended-ai/recommended-ai.component';
import { DataEngineeringComponent } from 'src/app/components/dashboard/data-engineering/data-engineering.component';
import { ProblemStatementComponent } from 'src/app/components/dashboard/problem-statement/problem-statement.component';
import { ModelEngineeringComponent } from 'src/app/components/dashboard/model-engineering/model-engineering.component';
import { DeployModelComponent } from 'src/app/components/dashboard/deploy-model/deploy-model.component';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { switchMap, tap, filter } from 'rxjs/operators';
import { EMPTY, throwError } from 'rxjs';
import { ViewDataComponent } from '../data-engineering/preprocess-data/view-data/view-data.component';
import { ConfirmationPopUpComponent } from '../data-engineering/preprocess-data/confirmation-pop-up/confirmation-popup.component';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { ExcelService } from 'src/app/_services/excel.service';
import { CookieService } from 'ngx-cookie-service';
import { DomSanitizer } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { HostListener } from '@angular/core';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { NotificationData } from 'src/app/_services/usernotification';
import * as $ from 'jquery';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { ApiService } from 'src/app/_services/api.service';
declare var userNotification: any;

@Component({
    selector: 'app-dashboard-panel',
    templateUrl: './dashboard-panel.component.html',
    styleUrls: ['./dashboard-panel.component.scss']
})
export class DashboardPanelComponent implements OnInit, OnDestroy {
    isPublicTemplates: string;
    cascadedId;
    redirectedFromCascadeModel;
    isnavBarToggle = false;
    isNavBarLabelsToggle = false;
    ispredefinedTemplate: boolean = false;
    correlationId;
    tableData;
    showDataColumnsList;
    problemTypeFlag = false;
    currentIndex = 0;
    isNextDisabled;
    isTimeSeries = false;
    modelName = '';
    allTabs;
    infoMessage;
    displayUploadandDataSourceBlock = 'false';
    isSaveAsSelected = false;
    newModelName;
    newCorrelationIdAfterCloned;
    breadcrumbIndex = 0;
    subscription: Subscription;
    isutility = false;
    isPreviousDisabled = false;
    readOnly;
    ifModelDeployed = 'false';
    isApplyDisabled = null;
    environmnet = '';
    ignoreAPICall: boolean = false;
    decimalPoint;

    @ViewChild(PstUseCaseDefinitionComponent, { static: false }) useCaseDefinitionComp: PstUseCaseDefinitionComponent;
    @ViewChild(DataCleanupComponent, { static: false }) dataCurationComp: DataCleanupComponent;
    @ViewChild(PreprocessDataComponent, { static: false }) dataTransformationComp: PreprocessDataComponent;
    @ViewChild(FeatureSelectionComponent, { static: false }) featureSelectionComp: FeatureSelectionComponent;
    @ViewChild(RecommendedAIComponent, { static: false }) recommendedAIComp: RecommendedAIComponent;
    @ViewChild(DataEngineeringComponent, { static: false }) dataEngineeringComp: DataEngineeringComponent;
    @ViewChild(ProblemStatementComponent, { static: false }) problemStatementComp: ProblemStatementComponent;
    @ViewChild(ModelEngineeringComponent, { static: false }) modelEngineeringComp: ModelEngineeringComponent;
    @ViewChild(DeployModelComponent, { static: false }) deployModelComp: DeployModelComponent;

    constructor(public router: Router, public problemStatementService: ProblemStatementService,
        public des: DataEngineeringService, private excelService: ExcelService,
        private appUtilsService: AppUtilsService, private ls: LocalStorageService,
        private ps: ProblemStatementService, private dialogService: DialogService,
        private ns: NotificationService, private route: ActivatedRoute, private cus: CoreUtilsService,
        private cookieService: CookieService,
        private domSanitizer: DomSanitizer,
        private envService: EnvironmentService,
        private api: ApiService) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
        this.route.queryParams
            .subscribe(params => {
                if (params.hasOwnProperty('cascadedId')) {
                    this.cascadedId = params.cascadedId;
                    if (params.hasOwnProperty('modelName')) {
                        this.modelName = params.modelName;
                    }
                }
                if (params.hasOwnProperty('displayUploadandDataSourceBlock')) {
                    if (sessionStorage.getItem('IsCascadeModelTemplate') == 'true') {
                        this.cascadedId = this.ls.getCorrelationId();
                    } else {
                        this.cascadedId = undefined;
                    }
                }
            });
        this.subscription = router.events.pipe(
            filter(event => event instanceof NavigationStart))
            .subscribe((event: NavigationStart) => {
                // You only receive NavigationStart events
                let url = event.url;
                if (url !== '' && this.redirectedFromCascadeModel !== undefined) {
                    if (url.includes('WhatIfAnalysis') === true && this.redirectedFromCascadeModel['redirected'] === true) {
                        this.allTabs = Object.assign(this.cus.allTabs);
                    }
                }
            });
        this.redirectedFromCascadeModel = Object.assign(this.cus.isCascadedModel);
    }
    ngOnInit() {
        // this.cascadedId = sessionStorage.getItem('cascadedId');
        this.environmnet = sessionStorage.getItem('Environment');
        this.readOnly = sessionStorage.getItem('viewEditAccess');
        if (sessionStorage.getItem('isModelDeployed') !== null) {
            this.ifModelDeployed = sessionStorage.getItem('isModelDeployed');
        }

        this.api.openDisclaimer();
        this.populateInfoMessage();
        this.correlationId = this.ls.getCorrelationId();
        this.allTabs = Object.assign(this.cus.allTabs);
        this.route.queryParams.subscribe(params => {
            if (params) {
                if (params.hasOwnProperty('modelName')) {
                    this.modelName = params.modelName;
                } else {
                    this.modelName = this.ls.getModelName();
                }
                if (params.hasOwnProperty('displayUploadandDataSourceBlock')) {
                    this.displayUploadandDataSourceBlock = params.displayUploadandDataSourceBlock;
                } else {
                    this.displayUploadandDataSourceBlock = 'false';
                }
            }
        });
        if (this.router.url.includes('usecasedefinition') === true) {
            this.currentIndex = 0;
            this.breadcrumbIndex = 0;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('datacleanup') === true) {
            this.currentIndex = 1;
            this.breadcrumbIndex = 0;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('preprocessdata') === true) {
            this.currentIndex = 1;
            this.breadcrumbIndex = 1;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('FeatureSelection') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 0;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('RecommendedAI') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 1;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('TeachAndTest') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 2;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('CompareModels') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 3;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('publishmodel') === true) {
            this.currentIndex = 3;
            this.breadcrumbIndex = 0;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        } else if (this.router.url.includes('deployedmodel') === true) {
            this.currentIndex = 3;
            this.breadcrumbIndex = 1;
            this.cus.disableTabs(this.currentIndex, this.breadcrumbIndex);
        }
        if (this.router.url.includes('utilities')) {
            this.isutility = true;
        }

    }


    @HostListener('window:beforeunload', ['$event'])
    onWindowClose(event) {
        if (sessionStorage.getItem('progress') === 'Inprocess') {
            event.preventDefault();
            event.returnValue = true;
        }
    }


    ngOnDestroy() {
        this.cascadedId = undefined;
        sessionStorage.removeItem('IsCascadeModelTemplate');
        this.cus.allTabs = [
            {
                'mainTab': 'Problem Statement', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'tabIndex': 0, 'subTab': [
                    { 'childTab': 'Use Case Definition', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'breadcrumbStatus': 'active' }
                ]
            },
            {
                'mainTab': 'Data Engineering', 'status': 'disabled', 'routerLink': 'dataengineering/datacleanup', 'tabIndex': 1, 'subTab': [
                    { 'childTab': 'Data Curation', 'status': 'active', 'routerLink': 'dataengineering/datacleanup', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Data Transformation', 'status': 'disabled', 'routerLink': 'dataengineering/preprocessdata', 'breadcrumbStatus': 'disabled' }
                ]
            },
            {
                'mainTab': 'Model Engineering', 'status': 'disabled', 'routerLink': 'modelengineering/FeatureSelection', 'tabIndex': 2, 'subTab': [
                    { 'childTab': 'Feature Selection', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Recommended AI', 'status': 'active', 'routerLink': 'modelengineering/RecommendedAI', 'breadcrumbStatus': 'disabled' },
                    { 'childTab': 'Teach and Test', 'status': 'disabled', 'routerLink': 'modelengineering/TeachAndTest', 'breadcrumbStatus': 'disabled' },
                    { 'childTab': 'Compare Test Scenarios', 'status': 'disabled', 'routerLink': 'modelengineering/CompareModels', 'breadcrumbStatus': 'disabled' }
                ]
            },
            {
                'mainTab': 'Deploy Model', 'status': 'disabled', 'routerLink': 'deploymodel/publishmodel', 'tabIndex': 3, 'subTab': [
                    { 'childTab': 'Publish Model', 'status': 'active', 'routerLink': 'deploymodel/publishmodel', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Deployed Model', 'status': 'disabled', 'routerLink': 'deploymodel/deployedmodel', 'breadcrumbStatus': 'disabled' }
                ]
            }
        ];
        sessionStorage.removeItem('isModelDeployed');
        sessionStorage.removeItem('isNewModel');
        sessionStorage.removeItem('isModelTrained');
        sessionStorage.removeItem('applyFlag');
        sessionStorage.removeItem('customCascadedId');
    }

    navigateToCascadeModel(redirectedFromModelTemp?) {
        if (redirectedFromModelTemp !== true) {
            this.ls.setLocalStorageData('correlationId', '');
            this.ls.setLocalStorageData('modelName', '');
        }
        this.cus.allTabs = [
            {
                'mainTab': 'Problem Statement', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'tabIndex': 0, 'subTab': [
                    { 'childTab': 'Use Case Definition', 'status': 'active', 'routerLink': 'problemstatement/usecasedefinition', 'breadcrumbStatus': 'active' }
                ]
            },
            {
                'mainTab': 'Data Engineering', 'status': 'disabled', 'routerLink': 'dataengineering/datacleanup', 'tabIndex': 1, 'subTab': [
                    { 'childTab': 'Data Curation', 'status': 'active', 'routerLink': 'dataengineering/datacleanup', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Data Transformation', 'status': 'disabled', 'routerLink': 'dataengineering/preprocessdata', 'breadcrumbStatus': 'disabled' }
                ]
            },
            {
                'mainTab': 'Model Engineering', 'status': 'disabled', 'routerLink': 'modelengineering/FeatureSelection', 'tabIndex': 2, 'subTab': [
                    { 'childTab': 'Feature Selection', 'status': 'active', 'routerLink': 'modelengineering/FeatureSelection', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Recommended AI', 'status': 'active', 'routerLink': 'modelengineering/RecommendedAI', 'breadcrumbStatus': 'disabled' },
                    { 'childTab': 'Teach and Test', 'status': 'disabled', 'routerLink': 'modelengineering/TeachAndTest', 'breadcrumbStatus': 'disabled' },
                    { 'childTab': 'Compare Test Scenarios', 'status': 'disabled', 'routerLink': 'modelengineering/CompareModels', 'breadcrumbStatus': 'disabled' }
                ]
            },
            {
                'mainTab': 'Deploy Model', 'status': 'disabled', 'routerLink': 'deploymodel/publishmodel', 'tabIndex': 3, 'subTab': [
                    { 'childTab': 'Publish Model', 'status': 'active', 'routerLink': 'deploymodel/publishmodel', 'breadcrumbStatus': 'active' },
                    { 'childTab': 'Deployed Model', 'status': 'disabled', 'routerLink': 'deploymodel/deployedmodel', 'breadcrumbStatus': 'disabled' }
                ]
            }
        ];
        this.allTabs = Object.assign(this.cus.allTabs);
        if (this.cascadedId === null || this.cascadedId === undefined) {
            this.route.queryParams
                .subscribe(params => {
                    if (params.hasOwnProperty('cascadedId')) {
                        this.cascadedId = params.cascadedId;
                        this.router.navigate(['dashboard/problemstatement/cascadeModels/mapCascadeModels'],
                            {
                                queryParams: {
                                    'cascadedId': this.cascadedId,
                                    'redirectedFromModelTemp': redirectedFromModelTemp
                                }
                            });
                    }
                });
        } else {
            this.router.navigate(['dashboard/problemstatement/cascadeModels/mapCascadeModels'],
                {
                    queryParams: {
                        'cascadedId': this.cascadedId,
                        'redirectedFromModelTemp': redirectedFromModelTemp
                    }
                });
        }
    }

    disableTabs(index, breadcrumbIndex?) {
        this.allTabs.forEach((tab, i) => {
            if (index === i) {
                this.allTabs[i].status = 'active';
                this.allTabs[i].subTab.forEach((subtab, j) => {
                    if (breadcrumbIndex === j) {
                        this.allTabs[i].subTab[j].breadcrumbStatus = 'active';
                    } else {
                        if (j < breadcrumbIndex) {
                            this.allTabs[i].subTab[j].breadcrumbStatus = 'completed';
                        } else {
                            this.allTabs[i].subTab[j].breadcrumbStatus = 'disabled';
                        }
                    }
                });
            } else {
                //  this.allTabs[i].status = '';
            }
        });
    }

    toggleNavBar() {
        this.isnavBarToggle = !this.isnavBarToggle;
    }

    toggleNavBarLabels() {
        this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
    }

    showData() {
        this.appUtilsService.loadingStarted();
        this.correlationId = this.ls.getCorrelationId();
        this.ps.getViewUploadedData(this.correlationId, this.decimalPoint).subscribe(data => {
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


    showDataDataTransformation() {
        // Bug 712725 Ingrain_StageTest_R2.1 - Sprint 2 - Unable to close the data quality screen by clicking on close button
        // Fix : Added loader
        this.appUtilsService.loadingStarted();
        this.correlationId = this.ls.getCorrelationId();
        this.des.getShowData(this.correlationId, 100, false, this.dataTransformationComp.modelType, this.decimalPoint).subscribe(da => {
            //  var da = this.x;
            this.tableData = da['InputData'] || [];
            const tableDataOfTimeSeries = da['TimeSeriesInputData'] || [];
            // this.showDataColumnsList = da['ColumnList'].replace('[', '').slice(0, -1).split(',').map(s => s.trim()) || [];
            this.showDataColumnsList = da['ColumnList'].replace('[', '').slice(0, -1).split(',').map(s => s.trimStart()) || [];
            this.appUtilsService.loadingEnded();
            this.dialogService.open(ShowDataComponent, {
                data: {
                    'tableData': this.tableData,
                    'tableDataOfTimeSeries': tableDataOfTimeSeries,
                    'columnList': this.showDataColumnsList,
                    'problemTypeFlag': this.problemTypeFlag
                }
            });
        }, error => {
            this.appUtilsService.loadingEnded();
            this.ns.error('Something went wrong to get show Data.');
        });
    }

    viewData() {
        const viewDataOnclose = this.dialogService.open(ViewDataComponent, {
            data: {
                'correlationId': this.ls.getCorrelationId(),
                'userId': this.appUtilsService.getCookies().UserId
            }
        }).afterClosed.subscribe();

    }


    downloadData() {
        // // this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
        // //  if (confirmationflag === true) {
        // const component = this;
        // userNotification.showUserNotificationModalPopup();
        // $(".notification-button-close").click(function () {
        //   component.dialogService.open(ConfirmationPopUpComponent, {}).afterClosed.subscribe(poupdata => {
        //     if (poupdata === true) {
        //       component.des.getShowData(component.correlationId, 0, true, component.dataTransformationComp.modelType).subscribe(da => {
        //         if (component.problemTypeFlag) {
        //           const tableDataforDownload = da['TimeSeriesInputData'] || [];
        //           component.excelService.exportAsExcelFileWithSheets(tableDataforDownload, 'DownloadedData');
        //         } else {
        //           const tableDataforDownload = da['InputData'] || [];
        //           component.excelService.exportAsExcelFile(tableDataforDownload, 'DownloadedData');
        //         }
        //       });
        //     }
        //   });
        //   //   }
        // });
        if (this.environmnet === 'FDS' || this.environmnet === 'PAM') {
            this.showUserNotificationforFDS();
        } else {
            this.showUserNotificationforSI();
        }
    }


    saveModel(confirmChangesPopup?) {
        this.isSaveAsSelected = false;
        if (this.router.url.includes('usecasedefinition') === true) {
            this.useCaseDefinitionComp.save();
            //  this.allTabs[this.currentIndex + 1].status = '';
        } else if (this.router.url.includes('datacleanup') === true) {
            if (!this.readOnly) {
                this.dataCurationComp.checkRetrainStatus();
            }
            this.isNextDisabled = this.dataCurationComp.nextDisabled;
            /* this.allTabs[1].subTab[0].breadcrumbStatus = 'completed';
            this.allTabs[1].subTab[1].breadcrumbStatus = ''; */
        } else if (this.router.url.includes('preprocessdata') === true) {
            this.dataTransformationComp.onSave();
            //  this.allTabs[this.currentIndex + 1].status = '';
        } else if (this.router.url.includes('FeatureSelection') === true) {
            if (!this.readOnly) {
                this.featureSelectionComp.checkRetrainStatus(confirmChangesPopup);
            }
            /* this.allTabs[2].subTab[0].breadcrumbStatus = 'completed';
            this.allTabs[2].subTab[1].breadcrumbStatus = ''; */
        } else if (this.router.url.includes('RecommendedAI') === true) {
            // this.recommendedAIComp.saveSelectedModels();
            /* this.allTabs[2].subTab[1].breadcrumbStatus = 'completed';
            this.allTabs[2].subTab[2].breadcrumbStatus = '';
            this.allTabs[this.currentIndex + 1].status = ''; */
        }
    }

    previous() {
        if (this.router.url.includes('usecasedefinition') === true) {
            this.currentIndex = 0;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.useCaseDefinitionComp.previous();
        } else if (this.router.url.includes('datacleanup') === true) {
            this.currentIndex = 0;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.dataCurationComp.previous();
        } else if (this.router.url.includes('preprocessdata') === true) {
            this.currentIndex = 1;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.dataTransformationComp.previous();
        } else if (this.router.url.includes('FeatureSelection') === true) {
            this.currentIndex = 1;
            this.breadcrumbIndex = 1;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.previous();
        } else if (this.router.url.includes('RecommendedAI') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.previous();
        } else if (this.router.url.includes('TeachAndTest') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 1;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.previous();
        } else if (this.router.url.includes('CompareModels') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 2;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.previous();
        } else if (this.router.url.includes('deploymodel') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 3;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.deployModelComp.previous();
        }
    }

    next(lessdataquality?) {
        if (this.router.url.includes('usecasedefinition') === true) {
            //  this.allTabs[this.currentIndex + 1].status = 'active';
            /* this.currentIndex = 1;
            this.breadcrumbIndex = 0;
            this.disableTabs(this.currentIndex, this.breadcrumbIndex); */
            if (this.ispredefinedTemplate === true && (this.useCaseDefinitionComp.isDirty.toLowerCase() === 'yes' || !this.ignoreAPICall)) {
                // condition added to avoid PostColumns API call when there is no changes made in useCaseDefinitionComp.
                this.saveModel();
            }
            this.useCaseDefinitionComp.next();
        } else if (this.router.url.includes('datacleanup') === true) {
            this.currentIndex = 1;
            this.breadcrumbIndex = 1;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.dataCurationComp.next(lessdataquality);
        } else if (this.router.url.includes('preprocessdata') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.dataTransformationComp.next();
        } else if (this.router.url.includes('FeatureSelection') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 1;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            if (this.featureSelectionComp.isFeatureSelectionSaved === true) {
                this.modelEngineeringComp.next();
            } else {
                this.ns.error('Kindly save before proceeding.')
            }
        } else if (this.router.url.includes('RecommendedAI') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 2;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.next();
        } else if (this.router.url.includes('TeachAndTest') === true) {
            this.currentIndex = 2;
            this.breadcrumbIndex = 3;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.next();
        } else if (this.router.url.includes('CompareModels') === true) {
            this.currentIndex = 3;
            this.breadcrumbIndex = 0;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.modelEngineeringComp.next();
        } else if (this.router.url.includes('deploymodel') === true) {
            this.currentIndex = 3;
            this.breadcrumbIndex = 1;
            //  this.disableTabs(this.currentIndex, this.breadcrumbIndex);
            this.deployModelComp.next();
        }
    }

    confirm() {
        if (this.router.url.includes('datacleanup') === true) {
            this.dataCurationComp.confirmPopup();
        }
        if (this.router.url.includes('FeatureSelection') === true) {
            this.featureSelectionComp.confirm();
        }
    }

    apply() {
        this.dataTransformationComp.checkRetrainStatus();
    }

    populateInfoMessage() {
        if (this.router.url.includes('usecasedefinition') === true) {
            this.infoMessage =
                `<div>In this page, user can define the problem statement that is to be addressed by ingrAIn Predictive model.</div>
      <div> Data can be uploaded by the user using file upload or through Phoenix data fabric and mapped to the respective fields.</div>
      <div>
      <b>Export File Password Hint:</b><br>
      &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
      &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
      </div>`;
        } else if (this.router.url.includes('datacleanup') === true) {
            this.infoMessage = `
      <div>In Data Curation, user gets an overview of the ingested data and it’s quality. </div>
      <div> User is suggested to perform actions on the attributes with low data quality </div>`;
        } else if (this.router.url.includes('preprocessdata') === true && this.environmnet !== 'FDS') {
            this.infoMessage = `
      <div>
      <b>Export File Password Hint:</b><br>
      &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
      &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
      </div>
      `;
        } else if (this.router.url.includes('FeatureSelection') === true) {
            this.infoMessage = `<div>Feature selection section provides option to the user to select/deselect features based on their importance.</div>
       <div>User can also modify the default training data % value and k-fold validations if required. </div>`;
        } else if (this.router.url.includes('RecommendedAI') === true) {
            let modelType = this.cookieService.get('ProblemType');
            if (modelType === 'Classification' || modelType === 'classification' || modelType === 'Multi_Class') {
                this.infoMessage = `
        <div>
        Types of multinomial classifications are:
        <ul>
        <li>
        <b>Decision Tree </b>: Decision Tree classifier is a classifier that has a structure similar to that of a tree.
        </li>
        <li>
        <b>Random Forest </b>: Random forest or Random decision forests are popular  ensemble methods that can be used to build predictive models for both classification and regression problems.
        </li>
        <li>
        <b>SVM Classifier </b>:The idea behind Support Vector Machine Classifier is to create a hyperplane that separates the datapoints belonging to different classes in such a way that minimum error is encountered when new datapoints are classified in future. 
        </li>
        <li>
        <b>K- Nearest Neighbor </b>: KNN is a non-parametric (no assumption of underlying distribution) and “lazy” learning (it does not generate a trained model; it predicts the class for new data points on the fly, considering all the previous data points) algorithm.
        </li>
        <li>
        <b>Naïve Bayes Classifier </b>: Bayes Classifier is based on Bayes Theorem
        </li>
        <li>
        <b>Stochastic Gradient Descent </b>: Stochastic gradient descent (often shortened in SGD) is a gradient descent optimization method for minimizing an objective function that is written as a sum of differentiable functions.
        </li>
        </ul>
        </div>
        `;
            } else if (modelType === 'Regression' || modelType === 'regression') {
                this.infoMessage = `
        <div>
        <ul>
         <li>
         <b>Ridge Regression </b>: Ridge regression is the regression type that is intended to be used in cases where the input dataset has the following properties:
          Large number of features in consideration
          Problem of collinearity; few of the features are not just correlated to the target variable but also to one another.
          The intention is to penalize certain features instead of completely removing them
         </li>
         <li>
          <b>Lasso Regression </b>: Very similar to Ridge regression, Lasso regression can be used in cases where the input dataset has the following properties:
          Large number of features in the input dataset.
          Issue of multi-collinearity; few of the features are not just correlated to the target variable but also to one another.
          The intention is to so strongly penalize certain features that the algorithm eventually completely removes them.
         </li>

         <li>
          <b>Elastic Net Regression </b>: Elastic Net regression combines the best features of both Ridge and Lasso regression by incorporating the penalty terms for both Ridge and Lasso equations in the Elastic Net regression equation.
         </li>
         
         <li>
          <b>Linear Regression </b>: Linear regression is probably the most intuitive and easy to implement regression technique.
         </li>
         <li>
          <b>Stepwise Regression </b>:-Stepwise regression is the step-by-step iterative construction of a regression model that involves automatic selection of independent variables.
         </li>
         <li>
         <b> Polynomial Regression</b>:-A regression equation is a polynomial regression equation if the power of independent variable is more than 1.
         </li>
         <li>
         <b> Logistic Regression </b>:Logistic regression is a variant of Linear Regression which is mainly used for assigning the data-points to various classes/labels.
         </li>
        </ul> 
       </div>`;
            } else if (modelType === 'timeseries' || modelType === 'TimeSeries') {
                this.infoMessage = `
        <div>
        <ul>
         <li>
         <b> ARIMA: </b> ARIMA stands for Auto Regressive Integrated Moving Average. It is actually a class of models that explains a given time series based on its own past values, that is, its own lags and the lagged forecast errors, so that equation can be used to forecast future values. Any ‘non-seasonal’ time series that exhibits patterns and is not a random white noise can be modeled with ARIMA models. 
         </li><li>
         <b> SARIMA:</b>  Seasonal Autoregressive Integrated Moving Average, SARIMA or Seasonal ARIMA, is an extension of ARIMA that explicitly supports univariate time series data with a seasonal component.
         </li><li>
         <b> Prophet:</b>  Prophet is a procedure for forecasting time series data based on an additive model where non-linear trends are fit with yearly, weekly, and daily seasonality, plus holiday effects. It works best with time series that have strong seasonal effects and several seasons of historical data.
         </li><li>
         <b> Exponential Smoothening:</b>  Exponential smoothing is a time series forecasting method for univariate data. In this technique, prediction is a weighted sum of past observations, but the model explicitly uses an exponentially decreasing weight for past observations.
         </li><li>
         <b> Holt-Winters: </b>  Holt-Winters method can be used to forecast data points in a series, provided that the series is “seasonal”, i.e. repetitive over some period.
         </li>
        </ul>
        </div> 
         `;

            }

        } else if (this.router.url.includes('HyperTuning')) {
            this.infoMessage = `
      <div>
      <b>	 What if Analysis </b>	: User is able to simulate various business scenarios on the trained model to see how the model is responding.
      <ul>
      <li>
        <b>	Predictive Analytics </b>: The technique helps the users to predict the future values of the Target attribute based on different scenarios of Input attributes.
      </li><li>
        <b>	Prescriptive Analytics</b> : The technique helps the user to identify the best course of action in order to achieve the desired value of Target Attribute.
      </li><li>
      <b>Download Template</b>: To test a scenario with multiple input records, download the template and fill in the values in the respective fields. Upload this template using the Update Test Data option
      </li><li>
      <b>Hyper Tuning</b>	: Technique of choosing a set of optimal hyperparameters for a model algorithm. Hyperparameter is a parameter whose value is used to control the learning algorithm. Recommended to be used by data scientist.
      </li>
      </ul>
      </div>`
        } else if (this.router.url.includes('TeachAndTest') === true) {
            if (this.environmnet === 'FDS') {
                this.infoMessage = `
      <div>
      <b>	 What if Analysis </b>	: User is able to simulate various business scenarios on the trained model to see how the model is responding.
      <ul>
      <li>
        <b>	Predictive Analytics </b>: The technique helps the users to predict the future values of the Target attribute based on different scenarios of Input attributes.
      </li><li>
        <b>	Prescriptive Analytics</b> : The technique helps the user to identify the best course of action in order to achieve the desired value of Target Attribute.
      </li><li>
      <b>Download Template</b>: To test a scenario with multiple input records, download the template and fill in the values in the respective fields. Upload this template using the Update Test Data option
      </li><li>
      <b>Hyper Tuning</b>	: Technique of choosing a set of optimal hyperparameters for a model algorithm. Hyperparameter is a parameter whose value is used to control the learning algorithm. Recommended to be used by data scientist.
      </li>
      </ul>
      </div>
       `
            } else {
                this.infoMessage = `
      <div>
      <b>	 What if Analysis </b>	: User is able to simulate various business scenarios on the trained model to see how the model is responding.
      <ul>
      <li>
        <b>	Predictive Analytics </b>: The technique helps the users to predict the future values of the Target attribute based on different scenarios of Input attributes.
      </li><li>
        <b>	Prescriptive Analytics</b> : The technique helps the user to identify the best course of action in order to achieve the desired value of Target Attribute.
      </li><li>
      <b>Download Template</b>: To test a scenario with multiple input records, download the template and fill in the values in the respective fields. Upload this template using the Update Test Data option
      </li><li>
      <b>Hyper Tuning</b>	: Technique of choosing a set of optimal hyperparameters for a model algorithm. Hyperparameter is a parameter whose value is used to control the learning algorithm. Recommended to be used by data scientist.
      </li>
      <li>
      <b>Export File Password Hint:</b><br>
      &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
      &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
      &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
      &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
      </li>
      </ul>
      </div>
       `
            }
        } else if (this.router.url.includes('CompareModels') === true) {
            this.infoMessage = `<div>The scenarios saved in Teach and Test section can be viewed and compared with one another in Compare Test Scenarios</div>`
        } else if (this.router.url.includes('publishmodel') === true) {
            this.infoMessage = `<div>The model training has been completed. </div> <div>User can now publish and deploy the model as Public/Private/Model Template in ingrAIn.</div><div> It can also be deployed to other applications by using Choose APP to Deploy option.</div>`;
        } else if (this.router.url.includes('deployedmodel') === true) {
            this.infoMessage = `<div>The deployed model details along with the Visualization, Web Services and App Link is available. User can navigate to the app where the model is deployed using the App Link</div>`;
        }

        this.infoMessage = this.domSanitizer.bypassSecurityTrustHtml(this.infoMessage);
    }

    panelClick(tabIndex, breadcrumbIndex?) {
        //debugger;
        let tab = this.allTabs[tabIndex];
        let subTab = this.allTabs[tabIndex].subTab[0];
        let isPublisModelTab;

        this.route.queryParams
            .subscribe(params => {
                if (tab.mainTab === 'Deploy Model') {
                    if (params.DataSource !== undefined) {
                        isPublisModelTab = true;
                    }
                }
                // this._appUtilsService.loadingEnded();
            });

        if (breadcrumbIndex) {
            subTab = this.allTabs[tabIndex].subTab[breadcrumbIndex];
        }
        if (tab.mainTab === 'Problem Statement') {
            const modelname = this.ls.getModelName();
            const modelCategory = this.ls.getModelCategory();
            let requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelName=' + modelname + '&pageSource=true';
            if (modelCategory !== null && modelCategory !== undefined && modelCategory !== '') {
                requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelCategory=' + modelCategory +
                    '&displayUploadandDataSourceBlock=false&modelName=' + modelname;
            }
            this.cus.disableTabs(tab.tabIndex, 0);
            this.router.navigateByUrl(requiredUrl);
        } else {
            let requiredUrl;
            if (breadcrumbIndex) {
                if (subTab.routerLink === 'deploymodel/deployedmodel') {
                    let datasource; let businessProblem;
                    if (this.problemStatementService.getColumnsdata !== undefined) {
                        if (this.problemStatementService.getColumnsdata.DataSource !== undefined) {
                            datasource = this.problemStatementService.getColumnsdata.DataSource;
                        } else {
                            datasource = sessionStorage.getItem('DataSource');
                        }
                        if (this.problemStatementService.getColumnsdata.DataSource !== undefined) {
                            businessProblem = this.problemStatementService.getColumnsdata.BusinessProblems;
                        } else {
                            businessProblem = sessionStorage.getItem('BusinessProblems');
                        }
                    }
                    requiredUrl = 'dashboard/' + subTab.routerLink + '?DataSource=' + datasource + '&BusinessProblems' + businessProblem;
                } else {
                    requiredUrl = 'dashboard/' + subTab.routerLink;
                }
                this.cus.disableTabs(tab.tabIndex, breadcrumbIndex);
            }
            else if (!breadcrumbIndex && isPublisModelTab) {
                requiredUrl = 'dashboard/deploymodel/publishmodel?publisModelFlag=true';
                this.cus.disableTabs(tab.tabIndex, 0);
            }

            else {
                requiredUrl = 'dashboard/' + tab.routerLink;
                this.cus.disableTabs(tab.tabIndex, 0);
            }
            this.router.navigateByUrl(requiredUrl);
        }
    }

    getcurrentmodelName(data) {
        this.modelName = data;
        this.ispredefinedTemplate = true;
    }

    onSaveAs() {
        this.isSaveAsSelected = false;

        if (this.newModelName !== null && this.newModelName !== '' && this.newModelName !== undefined) {
            this.appUtilsService.loadingStarted();
            this.correlationId = this.ls.getCorrelationId();
            this.modelName = this.modelName;
            this.problemStatementService.clone(this.correlationId, this.newModelName).subscribe(data => {
                if (data) {
                    this.newCorrelationIdAfterCloned = data;
                    //this.modelName = this.newModelName;
                    // let url = this.router.url.toString();
                    // this.router.navigate([url], {
                    //  queryParams: { modelName: this.newModelName, correlationId: data[0] }
                    // });
                    this.appUtilsService.loadingEnded();
                    this.ns.success('Cloned Successfully');
                }
            });
        } else {
            this.appUtilsService.loadingEnded();
            this.ns.error('Kindly enter model name.')
        }
    }



    onSaveAsValidation(modalName) {
        const valid = this.cus.isSpecialCharacter(modalName);
        if (valid === 0) {
            this.ns.error('Special charachters are not allowed.')
        } else {
            this.appUtilsService.loadingStarted();
            this.problemStatementService.getExistingModelName(modalName).subscribe(message => {
                if (message === false) {
                    this.appUtilsService.loadingEnded();
                    // this.setModelNameinLocalStorage(modalName);
                    this.onSaveAs();
                } else {
                    message = 'The model name already exists. Choose a different name.';
                    this.appUtilsService.loadingEnded();
                    this.ns.error(message);
                }
            }, error => {
                this.appUtilsService.loadingEnded();
                this.ns.error('Something went wrong.');
            });
        }
    }

    setModelNameinLocalStorage(modelName) {
        this.ls.setLocalStorageData('modelName', modelName);
    }

    toDisableNext(nextDisabled) {
        this.isNextDisabled = nextDisabled;
        this.isApplyDisabled = !nextDisabled;

        if (sessionStorage.getItem('isModelDeployed') === 'true' && this.isNextDisabled === true) {
            this.ifModelDeployed = 'true';
        }
    }

    toDisableApply(applyDisabled) {
        this.isApplyDisabled = applyDisabled ? applyDisabled : null;
    }

    checkTimeSeries(timeSeries) {
        this.isTimeSeries = timeSeries;
        this.problemTypeFlag = timeSeries;
    }

    toggleSaveAs() {
        this.isSaveAsSelected = !this.isSaveAsSelected;
    }

    decline() {
        this.featureSelectionComp.decline();
    }
    toDisablePrevious(previousDisabled) {
        this.isPreviousDisabled = previousDisabled;
    }

    showUserNotificationforFDS() {
        this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
            if (confirmationflag === true) {
                const component = this;
                component.dialogService.open(ConfirmationPopUpComponent, {}).afterClosed.subscribe(poupdata => {
                    if (poupdata === true) {
                        component.des.getShowData(component.ls.getCorrelationId(), 0, true, component.dataTransformationComp.modelType, this.decimalPoint).subscribe(da => {
                            let tableDataforDownload;
                            if (component.problemTypeFlag) {
                                const tableDataforDownload = da['TimeSeriesInputData'] || [];
                                component.excelService.exportAsExcelFileWithSheets(tableDataforDownload, 'DownloadedData');
                            } else {
                                const tableDataforDownload = da['InputData'] || [];
                                component.excelService.exportAsExcelFile(tableDataforDownload, 'DownloadedData');
                            }
                        });
                    }
                });
            }
        });
    }

    showUserNotificationforSI() {
        const component = this;
        userNotification.showUserNotificationModalPopup();
        $(".notification-button-close").click(function () {
            component.dialogService.open(ConfirmationPopUpComponent, {}).afterClosed.subscribe(poupdata => {
                if (poupdata === true) {
                    component.des.getShowData(component.ls.getCorrelationId(), 0, true, component.dataTransformationComp.modelType, this.decimalPoint).subscribe(da => {
                        let tableDataforDownload;
                        if (component.problemTypeFlag) {
                            tableDataforDownload = da['TimeSeriesInputData'] || [];
                            component.excelService.exportAsPasswordProtectedExcelFileWithSheets(tableDataforDownload, 'DownloadedData').subscribe(response => {
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
                        } else {
                            tableDataforDownload = da['InputData'] || [];
                            component.excelService.exportAsPasswordProtectedExcelFile(tableDataforDownload, 'DownloadedData').subscribe(response => {
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
                        }
                    });
                }
            });
        });
    }

    enableIgnoreAPICall(flag) {
        this.ignoreAPICall = flag;
    }

}
