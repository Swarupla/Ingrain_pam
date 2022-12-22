import { Component, OnInit, ElementRef, Inject, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { LoginService } from 'src/app/_services/login.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { Router, ActivatedRoute, Params } from '@angular/router';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { LetsgetstartedPopupComponent } from '../letsgetstarted-popup/letsgetstarted-popup.component';
import { StatusPopupComponent } from '../status-popup/status-popup.component';
import { ApiService } from 'src/app/_services/api.service';
import { Location } from '@angular/common';
import { TruncatePublicModelnamePipe } from 'src/app/_pipes/truncate-public-modelname.pipe';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { tap } from 'rxjs/operators';
import { interval } from 'rxjs';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';
declare var userNotification: any;

@Component({
    providers: [TruncatePublicModelnamePipe],
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {

    token: String;
    showMessage = true;
    logedinUserDetails: any;
    logedinUserRole: string;
    subscription: any;
    paramData: any;
    modelTrainingStatus: string;
    privateModelTrainingMsg: string;
    modelTrainingStatusText: string;

    clientUId: string;
    deliveryConstructUID: string;
    templates;
    templateModelNames;
    truncatedModelNames;
    truncatedmodelcorrelationid;
    modelCategory = '';
    modelName = '';
    technicaldebtuserIds = [];
    slauserIds = [];
    phcIds = [];
    userHasAccessRight = false;
    marketPlaceRedirectedUser = false;
    marketPlaceUserid: string;
    userId: string;
    showAcknowledgePopup: boolean;
    endPointUrl;
    messageStr;
    instanceName;
    WindowStatusServiceInterval: number = 10000;
    environmnet;
    instanceType: string;

    constructor(@Inject(ElementRef) private eleRef: ElementRef,
        private coreUtilsService: CoreUtilsService, private loginService: LoginService, private ls: LocalStorageService,
        private router: Router, private uts: UsageTrackingService, private ns: NotificationService,
        private probStatementService: ProblemStatementService, private dialogService: DialogService,
        private appUtilsService: AppUtilsService, private api: ApiService, private location: Location,
        private activatedRoute: ActivatedRoute, private truncateString: TruncatePublicModelnamePipe, private envService: EnvironmentService,
        private notificationService: NotificationService, private msalAuthentication: AuthenticationService) {
        console.log('home constructor');
        this.subscribeToConfirmAcknowledgmentButtonClick();
    }

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
            if (this.msalAuthentication.getAccount() === null) {
                this.msalAuthentication.login();
            } else {
                this.msalAuthentication.getToken().subscribe(data => {
                    if (data) {
                        this.envService.setMsalToken(data);
                    }
                });
            }
        }
        this.environmnet = sessionStorage.getItem('Environment');
        this.instanceName = sessionStorage.getItem('instanceName');
        this.envService.ingrainToken$.subscribe(data => {
            if (data) {
              this.loadDisclaimerPopup();
            }
          });

        this.activatedRoute.queryParams.subscribe((params: Params) => {
            if (params['fromApp'] === 'vds') {
                this.router.navigate(['dashboard/dataengineering/datacleanup/'], {
                    queryParams: params
                });
            }
        });
        // }
        setTimeout(() => {
            this.showMessage = false;
        }, 30000);

        this.ls.setLocalStorageData('modelName', '');
        this.ls.setLocalStorageData('modelCategory', '');

        this.ls.setMPLocalStorageData('modelNameMP', '');
        this.ls.setMPLocalStorageData('modelCategoryMP', '');

        this.logedinUserDetails = this.appUtilsService.getRoleData().subscribe(userDetails => {
            this.logedinUserRole = userDetails.accessRoleName;
        });

        this.subscription = this.appUtilsService.getParamData().subscribe(paramData => {
            this.paramData = paramData;
        });

        this.modelTrainingStatusText = this.envService.environment.modelTrainingStatusText;

        // to check marketplace registered user
        this.checkMarketPlacePreConfiguredUser();
        // check marketPlace preConfigured users

        //to get window service status
        this.GetWindowServiceStatus();
    }

    checkPageReload() {
        const isLoadedBefore = sessionStorage.getItem("IsProdApplicationLoaded");
        if (isLoadedBefore == "true") {
            return;
        }
        else {
            sessionStorage.setItem("IsProdApplicationLoaded", 'true');
            this.appUtilsService.loadingImmediateEnded();
            window.location.reload();
        }
    }

    getUserNotificationMessages() {
        this.probStatementService.getUserNotificationMessagesData()
            .subscribe((data: string) => {
                let jsonObject = {};
                jsonObject = data[0]["Contents"][0]["Message"];
                this.messageStr = JSON.stringify(jsonObject);
                this.messageStr = this.messageStr.replace(/"/g, "");
            });
    }

    confirmAcknowledge(appUnderstand: boolean) {
        localStorage.setItem('HasUserNotificationAccepted', 'true');
        var modal = document.getElementById("acknowledgeModal");
        if (modal) {
            modal.style.display = "none";
        }
    }

    subscribeToConfirmAcknowledgmentButtonClick() {
        window.addEventListener("storage", (e) => {
            if (localStorage.getItem("HasUserNotificationAccepted") != null && localStorage.getItem("HasUserNotificationAccepted") == "true") {
                this.confirmAcknowledge(null);
            }
        });
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    redirectToMarketPlace() {
        this.uts.usageTracking('Landing Page', 'Explore Guided Tour');
        window.open('https://mywizardingrainmarketplace-lx.accenture.com/');
    }

    redirectToDashboard() {
        this.uts.usageTracking('Landing Page', 'Lets Get started');
        // this.router.navigate(['/dashboard']);
        this.router.navigate(['choosefocusarea']);
    }

    closeDisclaimerPopup() {
        this.showMessage = false;
    }

    letsGetStarted() {
        this.appUtilsService.loadingStarted();
        if (this.logedinUserRole === 'System Admin' || this.logedinUserRole === 'Client Admin' ||
            this.logedinUserRole === 'System Data Admin' || this.logedinUserRole === 'Solution Architect') {

            this.subscription = this.appUtilsService.getParamData().subscribe(paramData => {
                this.paramData = paramData;

                this.probStatementService.getAppModelsTrainingStatus(this.paramData.clientUID, this.paramData.deliveryConstructUId, this.appUtilsService.getCookies().UserId).subscribe(status => {
                    if (status == "New" || status == "InProgress" || status == "Completed" || status == "CascadeNew" || status == "BothNew") {
                        this.modelTrainingStatus = status;
                        this.appUtilsService.loadingEnded();
                        if (this.modelTrainingStatus == "New" || status == "CascadeNew" || status == "BothNew") {
                            this.dialogService.open(LetsgetstartedPopupComponent, {}).afterClosed.subscribe(popupdata => {
                                if (popupdata === true) {
                                    if (this.modelTrainingStatus == "CascadeNew" || status == "BothNew") {
                                        this.probStatementService.getPrivateCascadeModelTraining(this.paramData.clientUID, this.paramData.deliveryConstructUId, this.appUtilsService.getCookies().UserId).subscribe(data => {
                                            if (data) {
                                                this.appUtilsService.loadingEnded();
                                            }
                                        }, error => {
                                            this.appUtilsService.loadingEnded();
                                        });
                                    }
                                    if (this.modelTrainingStatus == "New" || status == "BothNew") {
                                        this.probStatementService.getPrivateModelTraning(this.paramData.clientUID, this.paramData.deliveryConstructUId, this.appUtilsService.getCookies().UserId).subscribe(data => {
                                            if (data) {
                                                this.privateModelTrainingMsg = data.Message;
                                                this.openStatusPopup();
                                            }
                                        }, error => {
                                            this.ns.error('Error occurred: Due to some backend data process the relevant data could not be produced. Please try again while we troubleshoot the error.');
                                        });
                                    }
                                    this.appUtilsService.loadingEnded();
                                } else if (popupdata === false) {
                                    this.appUtilsService.loadingEnded();
                                } else {
                                    this.redirectToDashboard();
                                    this.appUtilsService.loadingEnded();
                                }
                            });
                        } else if (this.modelTrainingStatus == "InProgress") {
                            this.privateModelTrainingMsg = "Process Initiated";
                            this.openStatusPopup();
                            this.appUtilsService.loadingEnded();
                        } else if (this.modelTrainingStatus == "Completed") {
                            this.redirectToDashboard();
                            this.appUtilsService.loadingEnded();
                        }
                    } else {
                        this.ns.error('Error: ' + status);
                        this.redirectToDashboard();
                        this.appUtilsService.loadingEnded();
                    }
                }, error => {
                    this.appUtilsService.loadingEnded();
                    this.ns.error('Error occurred: Due to some backend data process the relevant data could not be produced. Please try again while we troubleshoot the error.');
                });

            });
        } else {
            this.redirectToDashboard();
            this.appUtilsService.loadingEnded();
        }
    }

    openStatusPopup() {
        this.dialogService.open(StatusPopupComponent, {
            data: {
                'privateModelTrainingMsg': this.privateModelTrainingMsg,
                'modelTrainingStatusText': this.modelTrainingStatusText
            }
        }).afterClosed.subscribe(statuspopupdata => {
            if (statuspopupdata === true) {
                this.redirectToDashboard();
            }
        });
    }

    // Start - Direct to public model (MarketPlace)
    navigateToPublicTemplates() {
        // // MarketPlace redirection for Pre-Configured Users
        this.truncatedModelNames = [];
        this.truncatedmodelcorrelationid = {};
        this.appUtilsService.loadingStarted();
        this.probStatementService.getPublicTemplatess(true, this.userId, this.modelCategory,
            null, this.deliveryConstructUID, this.clientUId).subscribe(
                data => {
                    const template = data;
                    console.log(template);
                    this.templates = data;
                    this.templateModelNames = this.templates.publicTemplates;
                    console.log(this.truncatedModelNames);
                    if (this.templateModelNames !== null) {
                        this.templateModelNames.forEach(i => {
                            this.truncatedModelNames.push(this.truncateString.transform(i.ModelName).trim());
                            this.truncatedmodelcorrelationid[this.truncateString.transform(i.ModelName)]
                                = [(i.CorrelationId), this.modelCategory];
                        });

                        // const truncatedModelDetails = Object.values(this.truncatedmodelcorrelationid);

                        if (this.truncatedModelNames.indexOf(this.modelName.trim()) > -1) {
                            const correlationId = this.truncatedmodelcorrelationid[this.modelName][0];
                            const tempCategory = this.truncatedmodelcorrelationid[this.modelName][1];
                            const tempModelName = this.modelName;
                            this.ls.setLocalStorageData('correlationId', correlationId);
                            this.ls.setLocalStorageData('modelName', tempModelName);
                            this.ls.setLocalStorageData('modelCategory', tempCategory);
                            this.navigateToUseCaseDefination(tempCategory, tempModelName);
                            this.appUtilsService.loadingEnded();
                        }

                    }
                });
    }

    checkMarketPlacePreConfiguredUser() {
        this.technicaldebtuserIds = [
            'mywizard.training1@accenture.com',
            'mywizard.training2@accenture.com',
            'mywizard.training3@accenture.com',
            'mywizard.training4@accenture.com',
            'mywizard.training5@accenture.com',
            'mywizard.training6@accenture.com',
            'mywizard.training7@accenture.com'
        ]

        this.slauserIds = [
            'mywizard.training8@accenture.com',
            'mywizard.training9@accenture.com',
            'mywizard.training10@accenture.com',
            'mywizard.training11@accenture.com',
            'mywizard.training12@accenture.com',
            'mywizard.training13@accenture.com',
            'mywizard.training14@accenture.com'
        ]

        this.phcIds = [
            'mywizard.training15@accenture.com',
            'mywizard.training16@accenture.com',
            'mywizard.training17@accenture.com',
            'mywizard.training18@accenture.com',
            'mywizard.training19@accenture.com',
            'mywizard.training20@accenture.com',
            'mywizard.training21@accenture.com'
        ]

        //  const userId = this.appUtilsService.getCookies().UserId;
        if (this.technicaldebtuserIds.indexOf(this.userId) > -1 || this.slauserIds.indexOf(this.userId) > -1 ||
            this.phcIds.indexOf(this.userId) > -1 || this.userHasAccessRight) {
            localStorage.setItem('marketPlaceTrialUser', 'True');
            this.isMyWizardTrainingUser(this.userId);
            this.navigateToPublicTemplates();
        } else if (localStorage.getItem('marketPlaceTrialUser') === 'True') {

        } else {
            localStorage.setItem('marketPlaceTrialUser', 'False');
        }
    }

    navigateToUseCaseDefination(modelCategory?, modelname?) {
        this.probStatementService.isPredefinedTemplate = 'True';
        this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
            {
                queryParams: {
                    'modelCategory': modelCategory,
                    'displayUploadandDataSourceBlock': true,
                    modelName: modelname
                }
            });
    }

    isMyWizardTrainingUser(userId: string) {
        if (this.technicaldebtuserIds.indexOf(userId) > -1) {
            this.modelCategory = 'Devops';
            // this.modelName = 'Technical Debt  ';
            this.modelName = 'Predict Technical Debt';
        }

        if (this.slauserIds.indexOf(userId) > -1) {
            this.modelCategory = 'AIops';
            this.modelName = 'Predict SLA';// 'SLA Prediction '; 
        }

        if (this.phcIds.indexOf(userId) > -1) {
            this.modelCategory = 'release Management';
            this.modelName = 'Proactive Alert on Project Health Status'; // 'Project Health Check      '; // 'Project Health Check_Tech Symposium';
        }
        // localStorage.setItem('marketPlaceTrialUser', 'True');
        if (localStorage.getItem('registeredMPUser') === 'True') {
            localStorage.setItem('registeredMPUser', 'False');
        }
        if (localStorage.getItem('marketPlaceRedirected') === 'True') {
            localStorage.setItem('marketPlaceRedirected', 'False');
        }
    }
    // End - Direct to public model

    //method to get window service status.
    GetWindowServiceStatus() {
        let genericErrorMessage = `Backend services are currently busy. Please try again in sometime`;
        interval(this.WindowStatusServiceInterval).subscribe(() => {
            this.probStatementService.getWindowServiceStatus().subscribe((response) => {
                if (response.Status === "STOPPED") {
                    this.notificationService.error(genericErrorMessage);
                }
            }, (error) => {
                this.notificationService.error(genericErrorMessage);
            })
        });
    }
    //End - method to get window service status.

    loadDisclaimerPopup() {
        if (this.environmnet === 'PAM' || this.environmnet === 'FDS') {
            if (this.router.url.includes('landingPage') === true) {
                const userNotificationpopup = this.dialogService.open(UserNotificationpopupComponent,
                    {
                    }).afterClosed.pipe(
                        tap()
                    );
                userNotificationpopup.subscribe();
            }

        } else {
            this.endPointUrl = "/v1/UserNotificationMessages";
            this.api.openDisclaimer();
            var hasUserNotificationAccepted = localStorage.getItem("HasUserNotificationAccepted");
            if (hasUserNotificationAccepted == 'false' || hasUserNotificationAccepted == null) {
                this.getUserNotificationMessages();
                this.showAcknowledgePopup = true;
                localStorage.setItem("ShowUserNotification", "true");
            }
            else {
                this.showAcknowledgePopup = false;
            }
        }
    }
}
