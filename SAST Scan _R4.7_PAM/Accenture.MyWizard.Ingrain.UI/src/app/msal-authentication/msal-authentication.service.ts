import { Injectable } from "@angular/core";
import { EnvironmentService } from "../_services/EnvironmentService";
import { MsalService } from "@azure/msal-angular";
import { AuthenticationResult, Configuration, EventType, LogLevel, PublicClientApplication } from "@azure/msal-browser";
import { Location } from "@angular/common";
import { Observable, of } from "rxjs";
import { map, retry, switchMap } from "rxjs/operators";
import { Token } from "../_models/Token";

@Injectable()
export class AuthenticationService {
    apiScopes: Array<string> = [];
    identityScopes: Array<string> = [];
    msalService: MsalService;
    constructor(private environmentService: EnvironmentService, private location: Location) { }
    bootstrap(): Promise<void> {
        return new Promise((resolve, reject) => {
            if (this.environmentService.environment.authProvider === 'AzureAD') {

                const msalConfig: Configuration = {
                    auth: {
                        clientId: this.environmentService.environment.clientId,
                        authority: 'https://login.microsoftonline.com/' + this.environmentService.environment.TenantId,
                        redirectUri: this.environmentService.environment.ingrainappUrl,
                        postLogoutRedirectUri: "https://login.microsoftonline.com/" + this.environmentService.environment.TenantId + "/oauth2/logout",
                    },
                    cache: {
                        cacheLocation: 'sessionStorage',
                        // storeAuthStateInCookie: isIE // set to true for IE 11
                    },
                    system: {
                        loggerOptions: {
                            loggerCallback(logLevel: LogLevel, message: string) {
                                console.log(logLevel + ' ' + message);
                            },
                            logLevel: LogLevel.Error,
                            piiLoggingEnabled: false
                        }
                    }

                };

                let publicClientApplication = new PublicClientApplication(msalConfig);
                console.log('msal config', publicClientApplication);
                this.msalService = new MsalService(publicClientApplication, this.location);
                resolve();
            }
            else if (this.environmentService.environment.authProvider === 'WindowsAuthProvider') {
                console.log("Windows auth initiated..");
                resolve();
            }
            else if (this.environmentService.environment.authProvider === 'Form') {
                console.log("Forms auth initiated..");
                resolve();
            } else if (this.environmentService.environment.authProvider === 'Local') {
                console.log("Forms auth initiated..");
                resolve();
            }
            reject();
        });
    };

    getToken(forceRefreshFlag = false): Observable<any> {
        if (this.environmentService.environment.authProvider === 'AzureAD') {
            this.apiScopes = [this.environmentService.environment.PhoenixResourceId];
            return this.msalService.acquireTokenSilent({
                scopes: this.apiScopes,
                forceRefresh: forceRefreshFlag,
                account: this.msalService.instance.getActiveAccount() == null ? this.msalService.instance.getAllAccounts[0] : this.msalService.instance.getActiveAccount()
            }).pipe(retry(3),
                switchMap((response: AuthenticationResult) => of(response)),
                map((response: AuthenticationResult): Token => ({ access_token: response.accessToken, expiresOn: response.expiresOn } as Token))
            );
        }
    }

    async login(): Promise<void> {
        if (this.environmentService.environment.authProvider === 'AzureAD') {
            this.identityScopes = ['user.read', 'openid', this.environmentService.environment.PhoenixResourceId];
            await this.msalService.loginRedirect({ scopes: this.identityScopes });
        }
    }

    logout(): void {
        if (this.environmentService.environment.authProvider === 'AzureAD') {
            this.msalService.logout();
        }
    }

    getAccount() {
        let user_account;
        if (this.environmentService.environment !== null && this.environmentService.environment.authProvider === 'AzureAD') {
            let account = this.msalService.instance.getActiveAccount();
            if (account == null) { return null; }
            else {
                console.log('msal account', account);
                user_account = account.username;
                return user_account;
            }
        }
        return null;
    }

}