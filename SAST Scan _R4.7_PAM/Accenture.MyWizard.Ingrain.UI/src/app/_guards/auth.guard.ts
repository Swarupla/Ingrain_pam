import { CanActivate, Router, CanDeactivate } from '@angular/router';
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AppUtilsService } from '../_services/app-utils.service';
import { LocalStorageService } from '../_services/local-storage.service';
import { CoreUtilsService } from '../_services/core-utils.service';
import { AuthenticationService } from '../msal-authentication/msal-authentication.service';
import { EnvironmentService } from '../_services/EnvironmentService';
import { Observable } from 'rxjs/Observable';
import { catchError, concatMap } from 'rxjs/operators';
import { of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NeedAuthGuard implements CanActivate {
    userCookie: any;
    constructor(private router: Router, private environmentService: EnvironmentService, private msalAuthentication: AuthenticationService, ) {
    }

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
        try {
          if (this.environmentService.environment.authProvider === 'AzureAD') {
            console.log('route',route);
            console.log('route url',this.router.url);
            return this.msalAuthentication.msalService.handleRedirectObservable()
              .pipe(concatMap(() => {
                if (!this.msalAuthentication.msalService.instance.getAllAccounts().length) {
                  if (state) {
                    console.log('AuthGuard - No accounts, redirect to login');
                    this.msalAuthentication.login();
                  }
                  else {
                    console.log('AuthGuard - No accounts, no State');
                    return of(false);
                  }
                }
                else {
                  let accounts = this.msalAuthentication.msalService.instance.getAllAccounts();
                  console.log('authguard getAll 1', accounts);
                  this.msalAuthentication.msalService.instance.setActiveAccount(accounts[0]);
                }
                let activeAccount = this.msalAuthentication.msalService.instance.getActiveAccount();
    
                if (!activeAccount && this.msalAuthentication.msalService.instance.getAllAccounts().length > 0) {
                  let accounts = this.msalAuthentication.msalService.instance.getAllAccounts();
                  console.log('authguard getAll 2', accounts);
                  this.msalAuthentication.msalService.instance.setActiveAccount(accounts[0]);
                  console.log('AuthGuard - Setting ' + accounts[0] + ' as active account.');
                  this.router.navigate(['login']);
                }
                console.log('AuthGuard - ' + this.msalAuthentication.msalService.instance.getAllAccounts());
                return of(true);
              }),
                catchError((error: Error) => {
                  console.error(error);
                  return of(false);
                }));
          } else if (this.environmentService.environment.authProvider.toLowerCase() === 'local'.toLowerCase()) {
            return of(true)
          } else if (this.environmentService.environment.authProvider.toLowerCase() === 'form'.toLowerCase()) {
            return of(true)
          }
    
        }
        catch (e) {
    
        }
        return of(false);
      }

}
