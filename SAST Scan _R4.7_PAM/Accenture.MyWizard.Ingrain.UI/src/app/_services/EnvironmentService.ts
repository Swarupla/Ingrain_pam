
/** Load Environment Config Files */

import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpBackend } from '@angular/common/http';
import 'rxjs/add/operator/toPromise';
import { Observable } from 'rxjs/Observable';
import { LocationStrategy } from '@angular/common';
import { environment } from '../../environments/environment';
import { map, catchError, retry } from 'rxjs/operators';
import {  Subject, throwError } from 'rxjs';
import { Token } from '../_models/Token';


@Injectable()
export class EnvironmentService {
    public environment: any = null;
    public environmentBaseUrl: any;

   // private _jsonURL = 'assets/Environment/config.json';

   private _jsonURL = 'assets/Environment/config.json';

    msalToken$: Subject<Token> = new Subject();
    ingrainToken$: Subject<any> = new Subject();
    isHeaderLoaded$: Subject<boolean> = new Subject();

    constructor(private http: HttpClient,
        private locationStrategy: LocationStrategy, private handler: HttpBackend) {
         if (this.locationStrategy.getBaseHref() !== '/') {
        this.environmentBaseUrl = this.locationStrategy.getBaseHref();
        } else {
        this.environmentBaseUrl = '';
        }

        console.log('Environment path-' + this.environmentBaseUrl + this._jsonURL);
    }
        // console.log('sdfsfsdfsdfsd'+this.environmentBaseUrl);

    public setEnviroinmentValues(val: any) {
        this.environment = val;
    }

    setMsalToken(token) {
        this.msalToken$.next(token);
    }

    setIngrainToken(token) {
        this.ingrainToken$.next(token);
    }

    public getEnviroinmentValues() {
        return this.environment;
    }

    loadConfig() {
            return new HttpClient(this.handler).get(this.environmentBaseUrl + this._jsonURL)
            .toPromise()
            .then(result => {
                this.environment = (result);                
            });
    }

    public isHeaderLoaded(){
        return this.isHeaderLoaded$;
    }

    public setHeaderLoaded(flag){
        this.isHeaderLoaded$.next(flag);
    }

    IsPADEnvironment(){
        const env = sessionStorage.getItem('Environment');
        const requestType = sessionStorage.getItem('RequestType');
        if ((env === 'PAM' || env === 'FDS') && (requestType === 'AM' || requestType === 'IO')) {
            return false;
        }else if(this.isTestDriveEnvironment()){
            return false;
        }else{
            return true;
        }
    }

    isTestDriveEnvironment(){
        const ingrainURL = this.environment.ingrainAPIURL.toLowerCase();
        if (ingrainURL.includes('stagept') || ingrainURL.includes('devtest') || ingrainURL.includes('devut') || ingrainURL.includes('uat') || ingrainURL.includes('stagetest')) {
            return false;
        }else{
            return true;
        }
    }

}
