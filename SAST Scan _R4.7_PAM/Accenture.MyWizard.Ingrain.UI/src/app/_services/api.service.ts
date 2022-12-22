import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpRequest, HttpEventType, HttpEvent, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, forkJoin, BehaviorSubject } from 'rxjs';
import { tap, map, last, retry } from 'rxjs/operators';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
declare var userNotification: any;

@Injectable({
    providedIn: 'root'
})


export class ApiService {
    private _jsonURL = 'assets/Environment/config.json';

    public fortressUrl;
    // public API_URL;
    public ingrainAPIURL;
    // public phoenixUrl;
    public mywizardHomeUrl;

    public phoenixWebBaseURL;
    public phoenixApiBaseURL;
    public AppServiceUID;
    public sessionTimeout;
    public warningPopupTime;
    public pingInterval;
    public AuthProvider;
    public pheonixApiCoreBaseUrl;
    public msTokenURL;
    public tokenGrantType;
    public tokenClientId;
    public tokenResourceId;
    public tokenClientSecret;
    public tokenScope;
    public authProvider;
    public vdsURL;
    public ingrainsignoutURL;
    public tenantId;
    public myConcertoHomeURL;
    private status;
    public ingrainappUrl;
    public FDSbaseURL;
    public PheonixTokenUrl;
    public SourceRedirectionURL;
    public azureLogoutURL;
    backButtonBehaviour: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
    IsPhoenixTokenAvailable$: BehaviorSubject<boolean> = new BehaviorSubject(false);

    constructor(private http: HttpClient, private envService: EnvironmentService) {
        this.fetchdetails();
    }

    fetchdetails() {
        if (this.envService.environment.authProvider.toLowerCase() === 'WindowsAuthProvider'.toLowerCase()) {
            this.azureLogoutURL = "https://login.microsoftonline.com/" + this.envService.environment['TenantId'] + "/oauth2/logout";
            this.ingrainAPIURL = this.envService.environment.ingrainAPIURL;
            this.fortressUrl = this.envService.environment.fortressUrl;
            this.mywizardHomeUrl = this.envService.environment.mywizardHomeUrl;
            this.phoenixWebBaseURL = this.envService.environment.myWizardWebConsoleUrl;
            this.phoenixApiBaseURL = this.envService.environment.myWizardAPIUrl;
            this.AppServiceUID = this.envService.environment.AppServiceUID;
            this.sessionTimeout = this.envService.environment.sessionTimeout;
            this.warningPopupTime = this.envService.environment.warningPopupTime;
            this.pingInterval = this.envService.environment.pingInterval;
            this.AuthProvider = this.envService.environment.authProvider;
            this.pheonixApiCoreBaseUrl = this.envService.environment.myWizardAPICoreUrl;
            this.msTokenURL = this.envService.environment.msTokenURL;
            this.tokenGrantType = this.envService.environment.grantType;
            this.tokenClientId = this.envService.environment.clientId;
            this.tokenResourceId = this.envService.environment.resourceId;
            this.tokenClientSecret = this.envService.environment.clientSecret;
            this.tokenScope = this.envService.environment.scope;
            this.authProvider = this.envService.environment.authProvider;
            this.vdsURL = this.envService.environment.vdsURL;
            this.ingrainsignoutURL = this.envService.environment.ingrainsignoutUrl;
            this.tenantId = this.envService.environment.TenantId;
            this.myConcertoHomeURL = this.envService.environment.myConcertoHomeURL;
            this.FDSbaseURL = this.envService.environment.FDSbaseURL;
            this.PheonixTokenUrl = this.envService.environment.PheonixTokenUrl;
            this.SourceRedirectionURL = this.envService.environment.SourceRedirectionURL;
        } else {
            const data = this.envService.environment;
            if (data) {
                this.azureLogoutURL = "https://login.microsoftonline.com/" + data['TenantId'] + "/oauth2/logout";
                this.fortressUrl = data['fortressUrl'];
                this.ingrainAPIURL = data['ingrainAPIURL'];
                // this.phoenixUrl = data['phoenixUrl'];
                this.mywizardHomeUrl = data['mywizardHomeUrl'];
                this.phoenixWebBaseURL = data['myWizardWebConsoleUrl'];
                this.phoenixApiBaseURL = data['myWizardAPIUrl'];
                this.AppServiceUID = data['AppServiceUID'];
                this.sessionTimeout = data['sessionTimeout'];
                this.warningPopupTime = data['warningPopupTime'];
                this.pingInterval = data['pingInterval'];
                this.AuthProvider = data['authProvider'];
                this.pheonixApiCoreBaseUrl = data['myWizardAPICoreUrl'];
                this.msTokenURL = data['token_Url'];
                this.tokenGrantType = data['grantType'];
                this.tokenClientId = data['clientId'];
                this.tokenResourceId = data['resourceId'];
                this.tokenClientSecret = data['clientSecret'];
                this.tokenScope = data['scope'];
                this.authProvider = data['authProvider'];
                this.vdsURL = data['vdsURL'];
                this.ingrainsignoutURL = data['ingrainsignoutUrl'];
                this.tenantId = data['TenantId'];
                this.myConcertoHomeURL = data['myConcertoHomeURL'];
                this.ingrainappUrl = data["ingrainappUrl"];
                this.FDSbaseURL = data["FDSbaseURL"];
                this.PheonixTokenUrl = data['PheonixTokenUrl'];
                this.SourceRedirectionURL = data['SourceRedirectionURL'];
            }
        }
    }

    getJSON() {
        return this.http.get(this._jsonURL) as Observable<any>;
    }


    get(path: string, data) {
        const url = `${this.ingrainAPIURL}${path}`;

        return this.http.get(url, { 'params': data }) as Observable<any>;
    }


    post(path: string, body, params?): Observable<any> {

        const url = `${this.ingrainAPIURL}${path}`;

        return this.http.post(url, body, { 'params': params }) as Observable<any>;
    }

    put(path: string, body, params): Observable<any> {

        const url = `${this.ingrainAPIURL}${path}`;

        return this.http.put(url, body, { 'params': params }) as Observable<any>;
    }

    delete(path: string, params): Observable<any> {

        const url = `${this.ingrainAPIURL}${path}`;

        return this.http.delete(url, { 'params': params }) as Observable<any>;
    }

    postFile(path, fileData, formData, params) {
        this.status = '';
        const url = `${this.ingrainAPIURL}${path}`;

        const request = new HttpRequest(
            'POST', url, formData,
            {
                'params': new HttpParams({ fromObject: params }),
                reportProgress: true
            });


        return this.http.request(request)
            .pipe(
                map(event => this.getEventMessage(event, fileData)),
            );
    }

    private getEventMessage(event: HttpEvent<any>, file) {
        let percentDone = 0;
        const body: any = '';
        switch (event.type) {
            case HttpEventType.Sent:
                return {
                    'message': `Uploading file '${file.name}' of size ${file.size}.`,
                    'percentDone': percentDone,
                    'body': body,
                    'status': this.status
                };

            case HttpEventType.UploadProgress:
                percentDone = Math.round(100 * event.loaded / event.total);
                return {
                    'message': `File '${file.name}' is ${percentDone}% uploaded.`,
                    'percentDone': percentDone,
                    'body': body,
                    'status': this.status
                };

            case HttpEventType.Response:
                return {
                    'message': `File '${file.name}' is ${percentDone}% uploaded.`,
                    'percentDone': percentDone,
                    'body': event.body,
                    'status': this.status
                };

            default:
                if (event.hasOwnProperty('status')) {
                    this.status = event['status'];
                }
                return {
                    'message': `File '${file.name}' surprising upload event: ${event.type}.`,
                    'percentDone': percentDone,
                    'body': body,
                    'status': this.status
                };

        }
    }

    // getDynamicData(path: string, data) {
    //   const url = `${this.headerURL}${path}`;
    //   return this.http.get(url, { 'params': data }) as Observable<any>;
    // }

    getPheonixAccountID(emailId) {
        let headers = new HttpHeaders();
        headers = headers.set('emailid', emailId);
        const url = `${this.phoenixApiBaseURL}${'/v1/Account'}`;
        return this.http.get(url,
            {
                headers: headers
            })
            .pipe(map(
                data => {
                    return data;
                },
                (error: HttpErrorResponse) =>
                    console.log(error)
            ));
    }

    getPheonix(path: string, data) {
        const url = `${this.phoenixApiBaseURL}${path}`;
        return this.http.get(url, { 'params': data }) as Observable<any>;
    }

    postPheonix(path: string, body, params?): Observable<any> {
        const url = `${this.phoenixApiBaseURL}${path}`;
        return this.http.post(url, body, { 'params': params }) as Observable<any>;
    }

    getPheonixToken(clientId, clientSecret, resourceId) {
        return this.get('IngrainAPIToken', { clientId: clientId, clientSecret: clientSecret, resourceId: resourceId });
    }

    initializeUserNotificationContent() {
        // console.log('user notification');     
        const content = {
            DataSourceUId: '00100000-0020-0000-0000-000000000000',
            TemplateUId: '00200000-0010-0000-0000-000000000000',
            ServiceUrl: 'assets/data/UserNotification.json',
            ActiveLanguage: 'en-US',
            BaseUrl: this.envService.environment.myWizardAPIUrl,
            EndPointUrl: '/v1/UserNotificationMessages',
            Token: sessionStorage.getItem('pheonixToken'),
            appServiceUId: this.envService.environment.AppServiceUID
        };
        return content;
    }


    openDisclaimer() {
        //  var notifi = new NotificationData();
        var content = this.initializeUserNotificationContent();
        userNotification.init(content);
        console.log(content);
    }

    getPAMToken() {
        return this.get('GetToken', {});
    }

    setPhoenixTokenAvailability(flag) {
        this.IsPhoenixTokenAvailable$.next(flag);
    }

    getPhoenixTokenAvailability() {
        return this.IsPhoenixTokenAvailable$;
    }
    /* For PAM */
    // getPheonixToken() {
    //   return this.get('IngrainAPIToken', {});
    // }
}
