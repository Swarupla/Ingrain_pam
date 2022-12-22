import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { StoreService } from '../../_services/store.service';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { AppUtilsService } from '../../_services/app-utils.service';
import { Router, ActivatedRoute, Params, NavigationStart } from '@angular/router';
import { ClientDeliveryStructureService } from '../../_services/client-delivery-structure.service';
import { CookieService } from 'ngx-cookie-service';
import { NotificationService } from '../../_services/notification-service.service';
export let browserRefresh = false;
import { interval, Subscription } from 'rxjs';
import { empty, throwError, timer, of } from 'rxjs';
import { tap, switchMap } from 'rxjs/operators';
import { JwtHelperService } from '../../_services/jwt-helper.service';
import * as moment from 'moment';
import * as _ from 'lodash';
const _moment = moment;
import { DialogService } from 'src/app/dialog/dialog.service';
import { SessionTimeoutPopupComponent } from '../header/session-timeout-popup/session-timeout-popup.component';
import { LocalStorageService } from '../../_services/local-storage.service';
import { Idle, DEFAULT_INTERRUPTSOURCES } from '@ng-idle/core';
import { Keepalive } from '@ng-idle/keepalive';
import { ApiService } from '../../_services/api.service';
import { Guid } from 'guid-typescript';
// import { RegressionPopupComponent } from 'src/app/components/dashboard/problem-statement/regression-popup/regression-popup.component';
import { DomSanitizer } from '@angular/platform-browser';
import { ModelsTrainingStatusPopupComponent } from '../header/models-training-status-popup/models-training-status-popup.component';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { VaConfigurationService } from 'virtual-agent-10-r4.0';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { error } from 'protractor';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';

// Object to pass to VAConfigurationService
const CONFIG_VA = {
    applicationName: 'ingrain',
    applicationUId: '',
    phoenixWebBaseURL: '',
    phoenixApiBaseURL: ''
};

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit, OnDestroy {
    @Input() isPheonixTokenAvailable: boolean;
    @Input() accountID;
    @Input() fullName;
    currentUser: any;
    id: number;
    userData;
    token: any;
    userId: string;
    clientUID: any;
    childUid: any;
    parentUID: any;
    userCookie: any;
    deliveryConstructData: any;
    logoutUrl: string;
    IsLoggedIn: boolean;
    authToken: any;
    browserRfereshload: boolean;
    subscription: any;
    sessionExpired: boolean;

    idleState = 'Not started.';
    timedOut = false;
    lastPing?: Date = null;
    isStyleAvailable: boolean;
    cssUrl: string;
    // phoenixUrl: string;
    mywizardHomeUrl: string;
    myWizardLogoUrl: any;
    accessRoleUIDVA = '';
    deliveryConstructUId_VA: any;
    clientUID_VA: any;
    environment;
    requestType;
    fromApp;
    isMyConcertoIntance = false;
    accentureLogoURL;
    myConcertoHomeURL: string;
    language: string;
    env: string;
    fromSourceSession;
    IsDownTimeNotificationAlert: boolean;
    DownTimeNotificationMessage;
    instanceType;
    ingrainAPIToken;

    constructor(private activatedRoute: ActivatedRoute, public router: Router, private sc: StoreService,
        private coreUtilsService: CoreUtilsService, private ns: NotificationService,
        private appUtilsService: AppUtilsService, private clientDelStructService: ClientDeliveryStructureService,
        private dialogService: DialogService, private localStorageService: LocalStorageService,
        private idle: Idle, private _apiService: ApiService,
        public sanitizer: DomSanitizer, private probStatementService: ProblemStatementService,
        private vaConfigService: VaConfigurationService,
        private envService: EnvironmentService, private msalAuthentication: AuthenticationService) {

        this.subscription = router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                browserRefresh = !router.navigated;
            }
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.isPheonixTokenAvailable?.currentValue != '' && changes.isPheonixTokenAvailable?.currentValue !== undefined) {
            if (changes.isPheonixTokenAvailable.currentValue === true) {
            }
        }
    }

    ngOnInit() {

        if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
            if (this.router.url.toString().indexOf('/landingPage') > -1) {
                return;
            } else {
                this.msalAuthentication.getToken().subscribe(data => {
                    if (data) {
                        this.envService.setMsalToken(data);
                    }
                });
            }
        }

        this.envService.ingrainToken$.subscribe(data => {
            if (data) {
                this.ingrainAPIToken = data;
                this.getPageloadAPI();
            }
        });
        this.env = this.envService.environment.authProvider.toLowerCase();
        this.mywizardHomeUrl = this._apiService.mywizardHomeUrl;
        this.myConcertoHomeURL = this._apiService.myConcertoHomeURL;

        if (sessionStorage.getItem('Environment') === 'FDS' || sessionStorage.getItem('Environment') === 'PAM') {
            this.environment = sessionStorage.getItem('Environment');
        }

        if (sessionStorage.getItem('RequestType') === 'AM' || sessionStorage.getItem('RequestType') === 'IO') {
            this.requestType = sessionStorage.getItem('RequestType');
        }

        if (localStorage.getItem('fromSource') === 'FDS' || localStorage.getItem('fromSource') === 'PAM') {
            this.fromSourceSession = localStorage.getItem('fromSource');
        }

        // INITIALIZE CONFIG_VA data
        CONFIG_VA.phoenixApiBaseURL = this._apiService.phoenixApiBaseURL;
        CONFIG_VA.phoenixWebBaseURL = this._apiService.phoenixWebBaseURL;
        CONFIG_VA.applicationUId = this._apiService.AppServiceUID;
        // INITIALIZE CONFIG_VA data

        const _this = this;
        this.sessionExpired = true;
        if (sessionStorage.getItem('uniqueId') === null) {
            const guid = Guid.create();
            sessionStorage.setItem('uniqueId', guid.toString());
        }

        _this.activatedRoute.queryParams.subscribe((params: Params) => {

            _this.userCookie = _this.appUtilsService.getCookies();
            _this.token = _this.userCookie.token;
            _this.userId = _this.userCookie.UserId;
            _this.userData = _this.sc.getUserData();

            /**Marketplace code to check if redirected URL is coming from MarketPlace - start */
            if (params.hasOwnProperty('isMPuser') || params.isMPuser === 'true') {
                localStorage.setItem('marketPlaceRedirected', 'True');
                if (params.modelName !== undefined) {
                    if (this.localStorageService.getMPCorrelationid() !== undefined) {
                        this.localStorageService.setMPLocalStorageData('correlationIdMP', this.localStorageService.getMPCorrelationid());
                    } else {
                        this.localStorageService.setMPLocalStorageData('correlationIdMP', params.correlationId);
                    }
                    this.localStorageService.setMPLocalStorageData('modelNameMP', params.modelName);
                    this.localStorageService.setMPLocalStorageData('modelCategoryMP', params.modelCategory);
                }
            }
            /**Marketplace code to check if redirected URL is coming from MarketPlace - end */

            if (_this.coreUtilsService.isNil(_this.userId)) {
                this.IsLoggedIn = false;
            } else {
                this.IsLoggedIn = true;
            }

            if (params['Environment'] === 'FDS' || params['Environment'] === 'PAM') {
                sessionStorage.setItem('Environment', params.Environment);
                sessionStorage.setItem('RequestType', params.RequestType);
                sessionStorage.setItem('ClientUId', params.ClientUId);
                sessionStorage.setItem('DeliveryConstructUId', params.DeliveryConstructUId);
                sessionStorage.setItem('End2EndId', params.End2EndId);
                if (params['Environment'] === 'FDS') {
                    sessionStorage.setItem('UserId', params.UserId + '@accenture.com');
                    sessionStorage.setItem('userId', params.UserId + '@accenture.com');
                    this.environment = 'FDS';
                } else if (params['Environment'] === 'PAM') {
                    // console.log('header component-- user ----', sessionStorage.getItem('UserID'))
                    this.environment = 'PAM';
                    if (params.hasOwnProperty('UserId')) {
                        sessionStorage.setItem('UserId', params.UserId);
                        sessionStorage.setItem('userId', params.UserId);
                    } else {
                        sessionStorage.setItem('UserId', sessionStorage.getItem('UserID'));
                        sessionStorage.setItem('userId', sessionStorage.getItem('UserID'));
                    }
                }
                this.requestType = params.RequestType;
            }

            /* if (params['fromApp'] === 'vds') {
              if (sessionStorage.getItem('QueryParams') !== null && sessionStorage.getItem('QueryParams') !== 'False') {
                const queryString = sessionStorage.getItem('QueryParams');
                sessionStorage.setItem('QueryParams', 'False');
                this.router.navigate(['datacleanup'], {
                  queryParams: params
                });
              }
            } */

            if (params['Type'] === 'Release Success Predictor') {
                this.router.navigate(['success-probability-visualization'],
                    {
                        queryParams: params
                    });
            }
            if (params['Type'] === 'Cascade Visualization') {
                if (params.hasOwnProperty('CascadedId')) {
                    this.router.navigate(['cascadeVisualization'],
                        {
                            queryParams: params
                        });
                }
            }
            if (params.hasOwnProperty('serviceName') && params.serviceName == 'Inference Engine') {
                console.log('inner redirection');
                this.router.navigate(['inferenceEngine'],
                    {
                        queryParams: params
                    });
            }

            if (!_this.coreUtilsService.isNil(params['ClientUId'])) {

                _this.appUtilsService.setParamData(params['DeliveryConstructUId'], params['ClientUId'], params['parentUID'], true, false);
                sessionStorage.removeItem('ClientUId');
                sessionStorage.removeItem('DeliveryConstructUId');
                sessionStorage.setItem('ClientUId', params['ClientUId']);
                sessionStorage.setItem('DeliveryConstructUId', params['DeliveryConstructUId']);
                if (!_this.coreUtilsService.isNil(params['fromApp'])) {
                    localStorage.setItem('fromApp', params['fromApp']);
                    this.fromApp = params['fromApp'];
                    this.localStorageService.setLocalStorageData('correlationId', params['CorrelationId']);
                }
                if (!_this.coreUtilsService.isNil(params['fromSource'])) {
                    localStorage.setItem('fromSource', params['fromSource']);
                }
                if (!_this.coreUtilsService.isNil(params['End2EndId'])) {
                    sessionStorage.setItem('End2EndId', params['End2EndId']);
                }
                this.appUtilsService.getParamData().subscribe(data => {

                    console.log('getParam1');

                    // Check if Params data value.
                    // If member deliveryConstructData is empty then call getAppExecutionContext,getClientDetails to setParamData
                    // If member deliveryConstructData data is not empty then ignore setting it again .

                    console.log('this.deliveryConstructData 1', this.deliveryConstructData);
                    if (this.coreUtilsService.isNil(this.deliveryConstructData)) {
                        this.deliveryConstructData = data;
                    }
                });
            } else {
                if (!this.coreUtilsService.isNil(sessionStorage.getItem('ClientUId')) &&
                    !this.coreUtilsService.isNil(sessionStorage.getItem('DeliveryConstructUId'))) {

                    _this.appUtilsService.setParamData(sessionStorage.getItem('DeliveryConstructUId'),
                        sessionStorage.getItem('ClientUId'), '', true, false);
                    this.appUtilsService.getParamData().subscribe(data => {

                        // Check if Params data value.
                        // If member deliveryConstructData is empty then call getAppExecutionContext,getClientDetails to setParamData
                        // If member deliveryConstructData data is not empty then ignore setting it again .

                        if (this.coreUtilsService.isNil(this.deliveryConstructData)) {
                            this.deliveryConstructData = data;
                        }
                    });
                }
            }
        });

        if (localStorage.getItem('marketPlaceRedirected') === 'True') {
            if (this.localStorageService.getMPCorrelationid() !== undefined) {
                this.navigateMPUserToUseCaseDefination();
            } else {
                this.router.navigate(['/choosefocusarea'],
                    {
                        queryParams: {
                            'isMPuser': true
                        }
                    });
            }
        }
    }

    getPageloadAPI() {
        console.log('latest code deployed');
        if (sessionStorage.getItem('Environment') !== 'FDS' && sessionStorage.getItem('fromSource') !== 'FDS' && sessionStorage.getItem('Environment') !== 'PAM') {
            this.setDecimalPoint();
        }
        if (!this.coreUtilsService.isNil(this.userId)) {
            sessionStorage.setItem('userId', this.userId);
            this.getAccountID();
        } else if (!this.coreUtilsService.isNil(sessionStorage.getItem('userId')) && sessionStorage.getItem('Environment') !== 'PAM') {
            this.getAccountID();
        }
        if (!this.coreUtilsService.isNil(sessionStorage.getItem('ClientUId')) &&
            !this.coreUtilsService.isNil(sessionStorage.getItem('DeliveryConstructUId'))) {
            this.getAppExecutionContext();
            const obj = {
                clientUID: this.deliveryConstructData.clientUID,
                deliveryConstructUId: this.deliveryConstructData.deliveryConstructUId
            };
            this.getClientDetails(obj);
            this.setVAConfigService(obj.deliveryConstructUId, obj.clientUID, this.userId);
        } else {
            if (this.envService.environment.authProvider.toLowerCase() !== 'WindowsAuthProvider'.toLowerCase()) {
                sessionStorage.removeItem('ClientUId');
                sessionStorage.removeItem('DeliveryConstructUId');

                this.appUtilsService.getParamData().subscribe(ParamData => {
                    // Check if ParamData value.
                    // If ParamData is empty then call getDeliveryConstructInDB to setParamData
                    // If ParamData is not empty then ignore setting again ParamData.
                    if (this.coreUtilsService.isNil(ParamData)) {

                        this.getDeliveryConstructInDB();

                    }
                });
            }
        }
        this.envService.setHeaderLoaded(true);
    }
    getAccountID() {
        this._apiService.getPheonixAccountID(this.userId).subscribe((data: any) => {
            if (data) {
                this.accountID = data.AccountId;
                this.fullName = data.LastName + ', ' + data.FirstName;
            }
        });
    }
    setDecimalPoint() {
        if (!this.coreUtilsService.isNil(sessionStorage.getItem('ClientUId')) &&
            !this.coreUtilsService.isNil(sessionStorage.getItem('DeliveryConstructUId'))) {
            this._apiService.get('GetDecimalPointPlacesValue', {
                ClientUId: sessionStorage.getItem('ClientUId'),
                DeliveryConstructUId: sessionStorage.getItem('DeliveryConstructUId'),
                UserEmail: this.userId
            }).subscribe(data => {
                if (data) {
                    const parsed = JSON.parse(data);
                    sessionStorage.setItem('decimalPoint', parsed[0].DecimalPlaces);
                }
            }, (error) => {
                if (error.error) {
                    console.log(error.error);
                }
            });
        }
    }


    ngOnDestroy() {
        this.subscription.unsubscribe();
    }
    getDeliveryConstructInDB() {
        console.log('getDeliveryConstructInDB');
        this.clientDelStructService.getDeliveryConstructs(this.userId).subscribe(
            data => {
                if (data) {
                    sessionStorage.setItem('userId', data.UserId);
                    sessionStorage.setItem('clientID', data.ClientUId);
                    sessionStorage.setItem('dcID', data.DeliveryConstructUID);
                    this.appUtilsService.getParamData().subscribe(paramdata => {
                        this.deliveryConstructData = paramdata;
                    });
                    if (this.coreUtilsService.isNil(this.deliveryConstructData.clientUID) && !this.coreUtilsService.isNil(data)) {
                        this.appUtilsService.setParamData(data['DeliveryConstructUID'], data['ClientUId'], data['parentUID'], false, true);

                        this.appUtilsService.getParamData().subscribe(paramdata => {
                            this.deliveryConstructData = paramdata;
                        });
                    }
                    if (this.coreUtilsService.isNil(this.deliveryConstructData.clientUID) && this.coreUtilsService.isNil(data)) {

                        this.appUtilsService.setParamData('', '00506000-0000-0000-1111-000000001234', '', false, false);

                        this.appUtilsService.getParamData().subscribe(paramdata => {
                            this.deliveryConstructData = paramdata;
                        });
                    }
                } else {
                    this.ns.error('Phoenix API is not responding. Please try after some time.');
                }
                this.getAppExecutionContext();
                const obj = {
                    clientUID: this.deliveryConstructData.clientUID,
                    deliveryConstructUId: this.deliveryConstructData.deliveryConstructUId
                };
                this.getClientDetails(obj);
                this.setVAConfigService(obj.deliveryConstructUId, obj.clientUID, this.userId);
            }, error => {
                this.ns.error('Something went wrong to mapping from collection');
            });
    }
    loadDataIfBrowserRefresh() {
        this.appUtilsService.getParamData().subscribe(data => {
            this.deliveryConstructData = data;
        });
    }
    logout() {
        document.cookie = "HasBrowserClosed=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
        this.appUtilsService.deleteCookies();
        if (this.envService.environment.authProvider === 'AzureAD') {
            this.msalAuthentication.logout();
        } else if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
            document.cookie = "AUTH_SESSION=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            console.log('document cookie inside header component - > ', document.cookie);
            console.log('localstorage data -> ', localStorage);
            console.log('sessionstorage data -> ', sessionStorage);
            this.idle.watch(false);
            window.location.href = this._apiService.ingrainsignoutURL;
        }

    }
    authirizeError(event: any) {
        if (event === '401') {
            sessionStorage.clear();
            this.logout();
        }
    }
    openSessionPopup() {
        sessionStorage.clear();
        const openModelSessionTimeout = this.dialogService.open(SessionTimeoutPopupComponent, {}).afterClosed.pipe(
            switchMap(closed => {
                if (!this.coreUtilsService.isNil(closed) && closed === 'closed') {
                    this.logout();
                    // tslint:disable-next-line: deprecation
                    return empty();
                }
            }),
            tap(data => {
                if (data) {
                    // this.notificationService.success('');
                }
            })
        );
        openModelSessionTimeout.subscribe();
    }
    reset() {
        this.idle.watch();
        this.idleState = 'Started.';
        this.timedOut = false;
    }

    getAppExecutionContext() {
        console.log('fromSourceSession' + this.fromSourceSession);
        if (this.environment === 'PAM' || this.environment === 'FDS' || this.fromSourceSession === 'FDS') {
            sessionStorage.setItem('instanceName', '');
            this.isMyConcertoIntance = false;
            this.myWizardLogoUrl = 'assets/images/mywiz.svg';
            this.isStyleAvailable = true;
            this.sc.sendValueToFooter(this.isStyleAvailable);
            /*Add Css for PAM in different file and attachec refrence */

            this.cssUrl = 'assets/styles/_PAM_HeaderFooter.scss';
            let element;
            element = document.createElement('link');
            element.rel = 'stylesheet';
            element.href = this.cssUrl;
            document.getElementsByTagName('head')[0].appendChild(element);
            this.isStyleAvailable = true;
            this.sc.sendValueToFooter(this.isStyleAvailable);
        } else {
            this.appUtilsService.loadingStarted();
            this.clientDelStructService.getAppExecutionContext(this.deliveryConstructData.clientUID,
                this.deliveryConstructData.deliveryConstructUId, this.userId).subscribe(response => {
                    sessionStorage.setItem('instanceName', response.InstanceName);
                    this.IsDownTimeNotificationAlert = response.IsDeploymentNotificationAlertEnabled;
                    this.DownTimeNotificationMessage = response.DeploymentNotificationMessage;
                    if (response.InstanceName === 'MyConcerto') {
                        this.isMyConcertoIntance = true;
                        this.myWizardLogoUrl = response.ImageFolderPath + 'logo.svg';
                        this.accentureLogoURL = response.ImageFolderPath + 'defaultlogo.svg';
                    } else {
                        this.isMyConcertoIntance = false;
                        this.myWizardLogoUrl = response.ImageFolderPath + 'my-wizard-logo.svg';
                    }
                    this.cssUrl = response.StyleUrl;
                    let element;
                    element = document.createElement('link');
                    element.rel = 'stylesheet';
                    element.href = this.cssUrl;
                    document.getElementsByTagName('head')[0].appendChild(element);
                    this.isStyleAvailable = true;
                    this.sc.sendValueToFooter(this.isStyleAvailable);
                    this.appUtilsService.loadingEnded();
                }, error => {
                    this.cssUrl = 'https://mywizard-devtest-lx.aiam-dh.com/css/MyWizard/style.css';
                    this.myWizardLogoUrl = 'https://mywizard-devtest-lx.aiam-dh.com/resources/MyWizard/images/my-wizard-logo.svg';

                    let element;
                    element = document.createElement('link');
                    element.rel = 'stylesheet';
                    element.href = this.cssUrl;
                    document.getElementsByTagName('head')[0].appendChild(element);
                    this.isStyleAvailable = true;
                    this.sc.sendValueToFooter(this.isStyleAvailable);
                    this.appUtilsService.loadingEnded();
                    this.displayPopUp(error);
                });
        }
    }

    private displayPopUp(error: any) {
        if (error.status === 401) {
            this.ns.error('token has expired So redirecting to login page');
            this.authirizeError('401');
        } else {
            this.ns.error('Phoenix API is not responding. Please try after some time.');
        }
    }

    getClientDetails(obj) {
        if (!this.router.url.includes('/video')) {
            this.appUtilsService.loadingStarted();
            if ((this.environment !== 'PAM' || this.environment !== 'FDS') && this.fromApp !== 'vds') {
                this.clientDelStructService.getClientDetails(obj.clientUID, obj.deliveryConstructUId, this.userId).subscribe(response => {
                    sessionStorage.setItem('ClientImageDetails', JSON.stringify(response));
                    this.getClientImage(obj.clientUID);
                    this.appUtilsService.loadingEnded();
                }, error => {
                    this.appUtilsService.loadingEnded();
                });
            } else { this.appUtilsService.loadingEnded(); }
        }
    }

    getClientImage(clientUID) {
        if (sessionStorage.getItem('ClientImageDetails') && clientUID) {
            const clientDetails = JSON.parse(sessionStorage.getItem('ClientImageDetails'));
            if (clientUID === clientDetails.ClientUId && clientDetails.ImageBinary) {
                sessionStorage.setItem('clientBinaryImg', clientDetails.ImageBinary);
            } else {
                sessionStorage.setItem('clientBinaryImg', 'assets/images/accenture-logo.png');
            }
        }

    }

    get clientBinaryImg() {
        const value = 'data:image/png;base64,';
        if (sessionStorage.getItem('clientBinaryImg')) {
            const imageBin = sessionStorage.getItem('clientBinaryImg');
            if (imageBin === 'assets/images/accenture-logo.png') {
                return imageBin;
            } else {
                return value + imageBin;
            }

        }
    }

    navigateBackToDashboard() {
        if (this.isMyConcertoIntance) {
            window.location.href = this.myConcertoHomeURL;
        } else {
            window.location.href = this.mywizardHomeUrl + '?ClientUId=' + this.deliveryConstructData.clientUID + '&DeliveryConstructUId=' + this.deliveryConstructData.deliveryConstructUId;
        }
        // window.open(this.mywizardHomeUrl);
    }

    navigateBackToVDS() {
        window.location.href = this._apiService.vdsURL + '?clientUId=' + this.deliveryConstructData.clientUID + '&accountUId=&deliveryConstructUId=' + this.deliveryConstructData.deliveryConstructUId + '';
    }

    /**
     * Redirect to VA from VDS only for Monte carlo redirection
     */
    navigateToVAForVDS() {
        const url = this._apiService.phoenixWebBaseURL + '/Appservices/00010020-0000-0000-0000-000000000000/virtualassistant/dashboardApp/00020120-0000-0000-0000-000000000000?instanceName=SI&ClientUId=' +
            this.deliveryConstructData.clientUID + '&DeliveryConstructUId=' + this.deliveryConstructData.deliveryConstructUId + '';
        window.open(url);
    }
    accessRoleUID(data) {
        this.accessRoleUIDVA = data;
    }

    /**
     * Function for VA Banner - To configure vaConfigService
     * @param dcId deliveryconstruct
     * @param clientID clientId
     * @param userId userId
     */
    private setVAConfigService(dcId: string, clientID: string, userId: string) {
        // Moving the Pheonix Token call to footer component
        // this.clientDelStructService.getPheonixTokenForVirtualAgent().subscribe(
        //   (result) => {
        //     if (!this.coreUtilsService.isNil(result)) {
        sessionStorage.getItem('pheonixToken');
        this.vaConfigService.configureVa(CONFIG_VA);
        this.deliveryConstructUId_VA = dcId;
        this.clientUID_VA = clientID;
        sessionStorage.setItem('userId', userId);
        sessionStorage.setItem('clientID', this.clientUID_VA);
        sessionStorage.setItem('dcID', this.deliveryConstructUId_VA);
        //   }
        // });
    }

    /**Marketplace registered users redirection to provisioned template */
    navigateMPUserToUseCaseDefination() {
        this.probStatementService.isPredefinedTemplate = 'True';
        this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
            {
                queryParams: {
                    'modelCategory': this.localStorageService.getMPModelCategory(),
                    'displayUploadandDataSourceBlock': true,
                    modelName: this.localStorageService.getMPModelName(),
                    'isMPuser': true,
                    'correlationId': this.localStorageService.getMPCorrelationid()
                }
            });
    }
}
