import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { tap, switchMap, catchError, delay, startWith, filter, pairwise } from 'rxjs/operators';
import { throwError, of, empty, Subject } from 'rxjs';
import { LoginService } from '../../_services/login.service';
import { NavigationStart, Router, ActivatedRoute, Params, RoutesRecognized } from '@angular/router';
import { timer, Subscription } from 'rxjs';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { AppUtilsService } from '../../_services/app-utils.service';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { ApiService } from '../../_services/api.service';
import { CookieService } from 'ngx-cookie-service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ClientDeliveryStructureService } from '../../_services/client-delivery-structure.service';
export let browserRefreshforApp = false;
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

import { Idle, DEFAULT_INTERRUPTSOURCES } from '@ng-idle/core';
import { Keepalive } from '@ng-idle/keepalive';
import { DialogService } from 'src/app/dialog/dialog.service';
import { SessionTimeoutPopupComponent } from '../header/session-timeout-popup/session-timeout-popup.component';
import { environment } from 'src/environments/environment';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';
import { Token } from 'src/app/_models/Token';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy, AfterViewInit {
  title = 'RoutingAndNavigation';
  spinnerVisible: boolean;
  authToken: string;
  subscription: Subscription;
  isPrivacyAccepted: boolean;
  public logoutUrl: any;
  tokenExpiryTime = 3900; // 3300;
  idleState = 'Not started.';
  timedOut = false;
  lastPing?: Date = null;
  idleTime; // = 3600;
  tokenSubscripton: Subscription;
  disableContinue = false;
  warningPopupTime; //: number;
  pingInterval; // = 15;
  openModelSessionTimeout;
  authprovider;
  refreshHandler: any;
  AzureADtokenRenewHandler: any;

  idleHandler: any;
  // New session timeout changes
  eventHandler: any;
  isHomePage = true;
  isUploadPage = false;
  display = 'none';
  isSessionTimeout = false;
  htmlConstantOk: string;
  htmlConstantCountinue: string;
  isSessionPopupOpen = false;
  popupMessage = '';
  popupButtonMessage = '';
  isSessionContinue: boolean = true;
  navSubscription: Subscription = null;
  msalToken: Token;

  // PAM variables
  pamTokenExpiry;
  ifTokenExtended = false;
  pamTokenTimeLeft: number = 480;
  accountID;
  fullName;
  userId;

  private dialogRef: MatDialogRef<SessionTimeoutPopupComponent>;
  env: string;

  isFDSEU = false;

  constructor(private router: Router, private apputilService: AppUtilsService,
    private activatedRoute: ActivatedRoute, private loginService: LoginService,
    private localStorageService: LocalStorageService, private coreUtilsService: CoreUtilsService,
    private apires: ApiService, private cookieService: CookieService, private _notificationService: NotificationService,
    private clientDelStructService: ClientDeliveryStructureService,
    private idle: Idle, private keepalive: Keepalive,
    public dialog: MatDialog,
    private _apiService: ApiService,
    private msalAuthentication: AuthenticationService,
    private envService: EnvironmentService,
    private modalService: NgbModal) {

    this.navSubscription = this.router.events
      .pipe(filter((evt: any) => evt instanceof RoutesRecognized), pairwise())
      .subscribe((events: RoutesRecognized[]) => {
        var isRedirectToLoginReq = localStorage.getItem("isRedirectToLoginReq");
        if (isRedirectToLoginReq != null && isRedirectToLoginReq == "true" && this.envService.environment.Environment !== 'PAM') {
          console.log('this.navSubscription this.logout()');
          this.logout();
        }
        else if (events && events[1] && events[1].urlAfterRedirects.includes('login')) {
          window.history.forward();
        }
      });

    this.subscription = router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        browserRefreshforApp = !router.navigated;
      }
    });

    //New Session timout changes
    this.eventHandler = this.updateLastUserActivityTime.bind(this);
    this.trackUserActivity();
    this.subscribeToContinueButtonClick();
    this.logoutEventListner();
  }

  ngOnInit() {   
    this.envService.msalToken$.subscribe(data => {
      if (data) {
        console.log('inside msal token subject', data);
        this.msalToken = data;  
        this.authToken = data['access_token'];
        console.log('MSAL Token Details -' + this.authToken);
        if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
          this.startIdleTimer();
          this.renewToken();
          this.getToken();
        }  else if (this.envService.environment.authProvider.toLowerCase() === 'WindowsAuthProvider'.toLowerCase()) {
          this.getToken();
        }        
        // else if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase() ||
        //   this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase()) {
        //   this.startIdleTimer();
        //   this.getToken();
        // }        
      
      }
    });

    if(this.activatedRoute.url.toString().includes('mywizardingraineu') || this.activatedRoute.url.toString().includes('saas')) {
      this.isFDSEU = true;
    }

    if (this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase()) {
      this.startIdleTimer();
      this.getToken();
    }

    if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
       this.startIdleTimer();
       this.getToken();
   }


    this.idleHandler = null;
    var isRedirectToLoginReq = localStorage.getItem("isRedirectToLoginReq");
    if (isRedirectToLoginReq != null && isRedirectToLoginReq == "true" && this.envService.environment.Environment !== 'PAM') {
      console.log('this.idleHandler this.logout()');
      this.logout();
    
    }

    this.trackUserActivity();
    this.logoutEventListner();
    localStorage.setItem('hasContinueClicked', 'false');
    localStorage.setItem('lastCalled', new Date().toLocaleString());

    this.htmlConstantOk = 'OK';
    this.htmlConstantCountinue = 'Continue';

    this.apires.backButtonBehaviour.subscribe(value => {
      if (value) {
        this.isHomePage = true;
        this.isUploadPage = false;
        window.location.href = '';
      }
    });

    var hasUserNotificationAccepted = localStorage.getItem("HasUserNotificationAccepted");
    if (hasUserNotificationAccepted == 'false' || hasUserNotificationAccepted == null) {
      localStorage.setItem('HasUserNotificationAccepted', 'false');
      localStorage.setItem('ShowUserNotification', 'true');
    }


    // const hasClientUID = this.router.url.includes('ClientUId');

    const isCookieExists = this.getCookie("HasBrowserClosed");
    if (isCookieExists == "") {
      window.localStorage.removeItem("HasUserNotificationAccepted");
      window.localStorage.removeItem("ShowUserNotification");
      document.cookie = "HasBrowserClosed=false;" + ";path=/" + "; secure";
    }

    /* window.addEventListener('storage', (event) => {
     // if (event.storageArea === localStorage) {
        const token = localStorage.getItem('pheonixToken');
        if (token === undefined) {
          this.logout();
        }
    //  }
    }, false); */
    const isSessionTimeout = this.cookieService.get('isSessionTimeout');
    console.log('isSessionTimeout inside App component : ->'+isSessionTimeout);
    if (isSessionTimeout && JSON.parse(isSessionTimeout)) {
      this.closeModalDialog();
    } else {
      this.authprovider = this.envService.environment.authProvider.toLowerCase();

      this.activatedRoute.queryParams.subscribe((params: Params) => {
        sessionStorage.setItem('env', params['Environment']);
        if (params['Environment'] === 'FDS' || params['Environment'] === 'PAM' || params['fromSource'] === 'PAM') {
          if (params['fromSource'] === 'PAM') {
            // console.log('app component Request type ------ fromSource', params.fromRequestType);
            sessionStorage.setItem('RequestType', params.fromRequestType);
            sessionStorage.setItem('Environment', params.fromSource);
          } else {
            sessionStorage.setItem('Environment', params.Environment);
            sessionStorage.setItem('RequestType', params.RequestType);          
          }
          sessionStorage.setItem('ClientUId', params.ClientUId);
          sessionStorage.setItem('DeliveryConstructUId', params.DeliveryConstructUId);
          sessionStorage.setItem('End2EndId', params.End2EndId);

          if (params['Environment'] === 'FDS') {
            sessionStorage.setItem('UserId', params.UserId + '@accenture.com');
            sessionStorage.setItem('userId', params.UserId + '@accenture.com');
          } else if (params['Environment'] === 'PAM' || params['fromSource'] === 'PAM') {
            if (params.hasOwnProperty('UserId')) {
              sessionStorage.setItem('UserId', params.UserId);
              sessionStorage.setItem('userId', params.UserId);
            } else {
              console.log('app component-- user ----', sessionStorage.getItem('UserID'))
              sessionStorage.setItem('UserId', sessionStorage.getItem('UserID'));
              sessionStorage.setItem('userId', sessionStorage.getItem('UserID'));
            }
          }else if (params['Instance'] === 'PAM') {

            sessionStorage.setItem('Instance', params['Instance']);
          }

        } else if (params.hasOwnProperty('CategoryType') && params.hasOwnProperty('type')) { /* Monte Carlo Redirection COde*/
          // CategoryType: "ADWaterfall Analytics"
          // ClientUId: "9337cb96-e997-4b1d-b44f-50d7a5358102"
          // DeliveryConstructUId: "6ec92667-32d5-43ad-9b96-1e4e284a0079"
          // RequestType: "Release Risk Predictor"
          // type: "existing"
          this.apputilService.loadingStarted();
          if (params.RequestType === 'Release Risk Predictor' && params.type === 'existing') {
            // ADSP Flow
            this.router.navigate(['RiskReleasePredictor'],
              {
                queryParams: params
              });
          } else if (params.type === 'new' && params.RequestType === '') {
            // Create a new use case flow
            this.router.navigate(['generic'],
              {
                queryParams: params
              });
          } else if (params.type === 'existing' && params.RequestType !== '') {
            // Created usecases flow
            // this.apiCore.setVDSUseCaseName(params.RequestType);
            this.router.navigate(['generic'],
              {
                queryParams: params
              });
          }
          this.apputilService.loadingEnded();
          /* Monte Carlo Redirection COde*/
        } else if (params.hasOwnProperty('Type') && params.hasOwnProperty('CascadedId')) {
          if (params.Type === 'Cascade Visualization') {
            this.router.navigate(['cascadeVisualization'],
              {
                queryParams: params
              });
          }
        } else if (params.hasOwnProperty('Type')) {
          if (params.Type === 'Release Success Predictor') {
            this.router.navigate(['success-probability-visualization'],
              {
                queryParams: params
              });
          }
        } else if (params.hasOwnProperty('serviceName') && params.serviceName == 'Inference Engine') {
          console.log('app component, inner redirection');
          this.router.navigate(['inferenceEngine'],
              {
                  queryParams: params
              });
        } else if (params['fromApp'] === 'vds') {
          if (params['fromSource'] === 'PAM') {
            console.log('app component-- user ----', sessionStorage.getItem('UserID'))
            sessionStorage.setItem('Environment', 'PAM');
            // sessionStorage.setItem('RequestType', params.fromRequestType);
            if (params.hasOwnProperty('UserId')) {
              sessionStorage.setItem('UserId', params.UserId);
              sessionStorage.setItem('userId', params.UserId);
              // sessionStorage.setItem('UserID', params.UserId);
            } else {
              sessionStorage.setItem('UserId', sessionStorage.getItem('UserID')) // params.UserId);
              sessionStorage.setItem('userId', sessionStorage.getItem('UserID')) // params.UserId);         

            }
          }
          this.router.navigate(['dashboard/dataengineering/datacleanup/'], {
            queryParams: params
          });
          //  }
        }
      });

      const urlWindows = window.location.href;
      if (urlWindows.indexOf('?') > -1 && urlWindows.indexOf('fromApp=vds') > -1) {
        const arrParams = urlWindows.split('?');
        const queryParams = arrParams[1].split('&');
        if (!this.coreUtilsService.isNil(queryParams)) {
          localStorage.setItem('BrowsRefresh', 'false');
          for (let index = 0; index < queryParams.length; index++) {
            if (queryParams[index].indexOf('ClientUId') > -1) {
              const splitParams = queryParams[index].split('=');
              sessionStorage.setItem('ClientUId', splitParams[1]);
            }
            if (queryParams[index].indexOf('DeliveryConstructUId') > -1) {
              const splitParams = queryParams[index].split('=');
              sessionStorage.setItem('DeliveryConstructUId', splitParams[1]);
            }
            if (queryParams[index].indexOf('fromApp') > -1) {
              const splitParams = queryParams[index].split('=');
              localStorage.setItem('fromApp', splitParams[1]);
            }
            if (queryParams[index].indexOf('CorrelationId') > -1) {
              const splitParams = queryParams[index].split('=');
              this.localStorageService.setLocalStorageData('correlationId', splitParams[1]);
            }
            if (queryParams[index].indexOf('fromSource') > -1) {
              const splitParams = queryParams[index].split('=');
              localStorage.setItem('fromSource', splitParams[1]);
              sessionStorage.setItem('fromSource', splitParams[1]);
            }
            if (queryParams[index].indexOf('End2EndId') > -1) {
              const splitParams = queryParams[index].split('=');
              sessionStorage.setItem('End2EndId', splitParams[1]);
            }
          }
        }
      }
    }
  }

  getCookie(message) {
    if (message) {
      let name = message + "=";
      let decodedCookie = decodeURIComponent(document.cookie);
      let ca = decodedCookie.split(';');
      for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
          c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
          return c.substring(name.length, c.length);
        }
      }
      return "";
    }
  }

  getAccountID() {
    let url = this.apires.ingrainappUrl;
    if (!(url.includes('mywizardingraineu')) && !(url.includes('saas'))) {
    this.userId = sessionStorage.getItem('UserID');
    this._apiService.getPheonixAccountID(this.userId).subscribe((data: any) => {
      if (data) {
        this.accountID = data.AccountId;
        this.fullName = data.LastName + ', ' + data.FirstName;
      }
    });
  }
  }


  getToken() {
    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase() ||
      this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase() ||
      this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
      if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) { // msal Token
        this.clientDelStructService.getPheonixTokenForVirtualAgent().subscribe(
          (result) => {
                if (!this.coreUtilsService.isNil(result)) {
                    localStorage.setItem('pheonixToken', result);
                    sessionStorage.setItem('pheonixToken', result);
                    this.envService.setIngrainToken(result);
                    this.apires.setPhoenixTokenAvailability(true);
                    this.apires.openDisclaimer();
                    if (!this.isFDSEU) {
                      this.getAccountID();
                    }
                }
          });
      } else if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
        if (this.envService.environment.Environment.toUpperCase() === 'PAM') {
      if (!this.coreUtilsService.isNil(sessionStorage.getItem('AuthSession'))) {
        var token = sessionStorage.getItem('AuthToken');
         localStorage.setItem('headerAuthToken', token);
           this.authToken = token;
          this.clientDelStructService.getPheonixTokenForVirtualAgent().subscribe(
            (result) => {
                  if (!this.coreUtilsService.isNil(result)) {
                      localStorage.setItem('pheonixToken', result);
                      sessionStorage.setItem('pheonixToken', result);
                      this.envService.setIngrainToken(result);
                     // this.apires.setPhoenixTokenAvailability(true);
                     // this.apires.openDisclaimer();
                     // this.getAccountID();
                  }
            });
    
    } else {     
            console.log('else part');
            this.idle.watch(false);
            window.location.href = this.apires.SourceRedirectionURL;          
  }
    }
       
      } else if (this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase()) {
       this.loginService.getToken().subscribe(token => {
       this.authToken = token;
         this.clientDelStructService.getPheonixTokenForVirtualAgent().subscribe(
              (result) => {
                  if (!this.coreUtilsService.isNil(result)) {
                      localStorage.setItem('pheonixToken', result);
                      sessionStorage.setItem('pheonixToken', result);
                      this.envService.setIngrainToken(result);
                      this.apires.setPhoenixTokenAvailability(true);
                      this.apires.openDisclaimer();
                      this.getAccountID();
                  }
            });
         }, error => {
            this._notificationService.error('Authorization token is not generated');
          });
       }
    } else if (this.envService.environment.authProvider.toLowerCase() === 'WindowsAuthProvider'.toLowerCase()) {
      console.log('inside WindowsAuthProvider function');
    }
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
    this.removeLocalStorage();
    if (this.navSubscription) {
      this.navSubscription.unsubscribe();
    }
  }

  ngAfterViewInit() {
    this.apputilService.getSpinnerSubject()
      .pipe(
        startWith(null),
        delay(0),
        tap(data => this.spinnerVisible = data),
        catchError(data => throwError(data))
      ).subscribe();
  }

  removeLocalStorage() {
    localStorage.removeItem('BrowsRefresh');
    localStorage.removeItem('ClientUId');
    localStorage.removeItem('HasUserNotificationAccepted');
    localStorage.removeItem('DeliveryConstructUId');
    localStorage.removeItem('fromApp');
    localStorage.removeItem('correlationId');
    localStorage.removeItem('fromSource');
  }

  privacyAccepted(value: boolean) {
    this.isPrivacyAccepted = value;
  }

  logout() {
    localStorage.removeItem("userEmailIdSync");
    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      sessionStorage.removeItem('UserID');
      localStorage.removeItem('userId');
      sessionStorage.clear();
      this.msalAuthentication.logout();
      window.location.replace(this.apires.azureLogoutURL);
    } 
    else if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
      document.cookie = "AUTH_SESSION=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
      this.idle.watch(false);
      window.location.href = this.apires.ingrainsignoutURL;
    }
    document.cookie = "HasBrowserClosed=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
  }

  /// Developer note : for windowsAuthentication there is no logout and session timeout
  startIdleTimer() {
    if (this.envService.environment.authProvider.toLowerCase() !== 'WindowsAuthProvider'.toLowerCase()) {
      console.log('idletimer2');
      this.warningPopupTime = 240; // 240 seconds. 4 minutes.   
      if (sessionStorage.getItem('Environment') === 'PAM') {
        this.idleTime = 840;
      } else {
        this.idleTime = this.envService.environment.sessionTimeout;
      }

      this.idle.setIdle(this.idleTime - this.warningPopupTime);
      // sets a timeout period of 3600 seconds - 60 min. after 3600 seconds - 1 hour of inactivity, the user will be considered timed out.
      this.idle.setTimeout(false);
      this.idle.setInterrupts(DEFAULT_INTERRUPTSOURCES);

      this.idle.onIdleEnd.subscribe(() => {
        this.idleState = 'No longer idle.';
        console.log('idle state ' + this.idleState + ' last ping ' + this.lastPing);
        this.reset();
      });
      this.idle.onTimeout.subscribe(() => {
        this.idleState = 'Timed out!';
        this.timedOut = true;
      });
      this.idleHandler = window.setInterval(() => {
        let lastCalled = localStorage.getItem("lastCalled");
        let newDate = new Date(lastCalled);
        var dt = (new Date()).valueOf() - (newDate.valueOf());
        var idealTimeInMin = parseInt(this.idleTime) / 60;
        var dtnew = ((idealTimeInMin - 4) * 60 * 1000); // 56

        if (dt >= dtnew && (dt < (idealTimeInMin * 60 * 1000)) && !this.isSessionPopupOpen) {
          this.popupMessage = 'Your online session has ended due to inactivity';
          this.popupButtonMessage = 'If you want to extend click on Continue button otherwise click on End Session.';
          // localStorage.setItem("isRedirectToLoginReq", "true");
          if (document.getElementsByClassName('mystyle').item(0) !== null) {
            document.getElementsByClassName('mystyle').item(0).remove();
          }
          this.modalService.dismissAll();
          this.isSessionTimeout = true;
          this.display = 'block';
          this.removeUserActivityTracker();
          this.eventHandler = null;
          this.isSessionPopupOpen = true;
          this.setCountinueButton();
        }
      }, 1000);

      this.idle.onTimeoutWarning.subscribe((countdown) => {
        this.idleState = 'You will time out in ' + countdown + ' seconds!';
      });

      // sets the ping interval to 15 seconds
      this.keepalive.interval(15);
      this.keepalive.onPing.subscribe(() => this.lastPing = new Date());
      this.reset();
    }
  }

  openSessionPopUp() {
    this.popupMessage = 'Your online session has ended due to inactivity';
    this.popupButtonMessage = 'If you want to extend click on Continue button otherwise click on End Session.';
    this.setCountinueButton();
    if (document.getElementsByClassName('mystyle').item(0) !== null) {
      document.getElementsByClassName('mystyle').item(0).remove();
    }
    this.modalService.dismissAll();
    this.isSessionTimeout = true;
    this.display = 'block';
    this.isSessionPopupOpen = true;
    this.idle.stop();
    this.reset();
  }

  renewToken() {
    // this.AzureADtokenRenewHandler = setInterval(() => {
    //   this.msalAuthentication.getToken(true)
    //     .subscribe((data: Token) => {
    //       console.log("Token refresh done");
    //       setInterval(this.AzureADtokenRenewHandler, this.calculateExpiry(data) * 60 * 1000);
    //     }, (error: any) => {
    //       console.log("Error in Token refreshing.");
    //     });
    // }); //convert to miliseconds // before expired 40 secoons token has been renewed
  }

  calculateExpiry(token: Token) {
    if (this.envService.environment.IsAzureTokenRefresh === true) {
      return this.envService.environment.AzureTokenRefreshTime;
    } else if (this.calculateExpiryTimeForAdToken(token) > 0) {
      return this.calculateExpiryTimeForAdToken(token);
    } else {
      return this.envService.environment.AzureTokenRefreshTime; // Default Value : 30 mins
    }
  }

  // here we will refresh token 9 min before actual expiration.
  calculateExpiryTimeForAdToken(token: Token) {
    const actualTokenExpiryTime = token.expiresOn;
    const expiryDate = new Date(actualTokenExpiryTime);
    expiryDate.setMinutes(expiryDate.getMinutes() - 9);
    const tokenExpiredIn = Math.round((expiryDate.valueOf() - (new Date()).valueOf()) / 60000);
    console.log('token exp time' + tokenExpiredIn);
    return tokenExpiredIn;
  }

  updateLastUserActivityTime() {
    localStorage.setItem('lastCalled', new Date().toLocaleString());
  }

  trackUserActivity() {
    window.addEventListener('mousedown', this.eventHandler);
    window.addEventListener('scroll', this.eventHandler);
    window.addEventListener('keydown', this.eventHandler);
  }

  removeUserActivityTracker() {
    window.removeEventListener('mousedown', this.eventHandler);
    window.removeEventListener('scroll', this.eventHandler);
    window.removeEventListener('keydown', this.eventHandler);
  }

  private subscribeToContinueButtonClick() {
    window.addEventListener('storage', (e) => {
      if ((localStorage.getItem('hasContinueClicked') != null && localStorage.getItem('hasContinueClicked') === 'true')) {
        // this.display = 'none';
        // this.isSessionPopupOpen = false;
        // this.eventHandler = this.updateLastUserActivityTime.bind(this);
        // this.trackUserActivity();
        // setTimeout(() => {
        //   localStorage.setItem('hasContinueClicked', 'false');
        // }, 2000);
        this.RefreshSession();
      }
    });
  }

  setCountinueButton() {
    this.isSessionContinue = true;
    setTimeout(() => {
      this.isSessionContinue = false;
      this.popupMessage = 'Your online session has ended due to inactivity.';
      this.popupButtonMessage = 'As a security precaution, you are now required to Sign In again';
    }, 240000);
  }

  RefreshSession() {
    this.display = 'none';
    this.isSessionPopupOpen = false;
    localStorage.setItem('lastCalled', new Date().toLocaleString());
    localStorage.setItem('hasContinueClicked', 'true');
    if (this.msalToken) {
      this.msalAuthentication.getToken(true)
        .subscribe((data: any) => {
          console.log("forcedSignin done RefreshSession");
        }, (error: any) => {
          console.log("Error in forcedSignin RefreshSession." + error);
        });
    }
    this.eventHandler = this.updateLastUserActivityTime.bind(this);
    this.trackUserActivity();
    this.idle.stop();
    this.reset();
  }

  closeModalDialog() {
    localStorage.removeItem('userEmailIdSync');
    sessionStorage.clear();
    localStorage.clear();
    localStorage.setItem('singin', 'true');

    this.isSessionTimeout = false;
    this.cookieService.set('isSessionTimeout', JSON.stringify(this.isSessionTimeout));
    this.display = 'none'; // set none css after close dialog
    localStorage.setItem("isRedirectToLoginReq", "true");

    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      if (this.msalToken) {
        this.msalAuthentication.logout();
      }
      let loginBaseURL = this.apires.msTokenURL.split('/token');
      let loginURL = loginBaseURL[0] + '/logout?post_logout_redirect_uri=' + this.apires.ingrainappUrl;
      window.location.href = loginURL;
    } else if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
      if (sessionStorage.getItem('Environment') === 'PAM') {
        this.idle.watch(false);
        window.location.href = this.apires.ingrainsignoutURL;
      }
    }
  }

  reset() {
    this.idle.watch(false);
    this.idleState = 'Started.';
    this.timedOut = false;
  }

  endsession() {
    localStorage.removeItem('userEmailIdSync');
    document.cookie = "HasBrowserClosed=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
    this.apputilService.deleteCookies();
    localStorage.setItem('logout', 'true');
    // localStorage.setItem("isRedirectToLoginReq", "true");
    // this.envService.environment.authProvider = 'AzureAD';
    if (this.envService.environment.authProvider === 'AzureAD') {
      this.msalAuthentication.logout();
      window.location.replace(this.apires.azureLogoutURL);
    }
  }

  private logoutEventListner() {
    window.addEventListener('storage', (event) => {
      if (event.key == 'logout' && event.newValue) {
        sessionStorage.removeItem('UserID');
        localStorage.removeItem('userId');
        this.logout();
       console.log('logoutEventListner this.logout()');
      }
    });

    // if (localStorage.getItem('singin') === 'true') {
    window.addEventListener('storage', (event) => {
        if (event.storageArea == localStorage) {
            let hasSigninClicked = sessionStorage.getItem('hasSigninClicked');
            // let loggedInUserEmail = localStorage.getItem('userEmailIdSync'); //userEmailIdSyncSIHome
            let loggedInUserEmail = sessionStorage.getItem('UserID') ? sessionStorage.getItem('UserID') : sessionStorage.getItem('userId');
            let env = sessionStorage.getItem('Environment') ? sessionStorage.getItem('Environment') : sessionStorage.getItem('fromSource');
            if ((loggedInUserEmail == undefined || loggedInUserEmail == "") && env !== 'FDS' && env !== 'PAM') {
                this.logout();
              console.log('window.addEventListener this.logout()');
            }
      }
    });
    //}
  }

  getPAMToken() {
    this.apires.getPAMToken().subscribe(res => {
      console.log('app component getPAMToken ------', res);
      if (res !== undefined) {
        const token = res.token;
        this.tokenExpiryTime = 900;
        this.authToken = token;
        localStorage.setItem('headerAuthToken', this.authToken);
        this.ifTokenExtended = true;
        this.renewPAMToken();
      }
    }, error => {
      this._notificationService.error('Authorization token is not generated');
    });
  }

  renewPAMToken() {
    // debugger;
    let timeLeft = this.pamTokenTimeLeft;
    this.pamTokenExpiry = setInterval(() => {
      if (timeLeft > 0) {
        timeLeft--;
      }
      // console.log('--', timeLeft);
      // if (timeLeft == this.warningPopupTime) {
      if (this.idleState === this.warningPopupTime) {
        if (timeLeft === this.warningPopupTime) {
          this.stopTokenRenewal(); timeLeft = this.pamTokenTimeLeft;
          // console.log('after', this.pamTokenExpiry);
        }
      } else if (timeLeft === 240) {
        this.stopTokenRenewal();
        this.getPAMToken();
      }
    }, 1000);

  }

  stopTokenRenewal() {
    clearInterval(this.pamTokenExpiry);
  }
}
