import { Injectable } from '@angular/core';
import { ApiService } from './api.service';
import { User } from '../_models/User';
import { map, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { CoreUtilsService } from './core-utils.service';


@Injectable({
  providedIn: 'root'
})
export class LoginService {

  tokenExpiryTime;

  constructor(private api: ApiService, private http: HttpClient,
    private cookieService: CookieService,
    private coreutilServices: CoreUtilsService) { }

  login(userData: User) {
    return this.api.post('ValidateUser', userData).pipe(
      map(user => {
        if (user) {
          // check for user token in future
          localStorage.setItem('currentUser', JSON.stringify(user));
        }
        return user;
      }));
  }

  logout() {
    localStorage.removeItem('currentUser');
  }

  getToken() {
    let token: any;
    let url;
    let params;
    if (this.api.authProvider.toLowerCase() === 'azuread' || this.api.authProvider.toLowerCase() === 'local') {
      return this.api.getPheonixToken(this.api.tokenClientId, this.api.tokenClientSecret, this.api.tokenResourceId)
        .pipe(
          map((response) => {
            if (response['access_token'] !== null) {
              token = response['access_token']; // fetch Access token from array.
              localStorage.setItem('headerAuthToken', token);
              this.tokenExpiryTime = response['expires_in'];
            }
            return response;
          }, error => {
            console.log('Login service - Error Section');
          }));
    } else if (this.api.authProvider.toLowerCase() === 'Form' || this.api.authProvider.toLowerCase() === 'local') {
      console.log('Login service - Form condition');
      // uncomment below line of code for PAM once API is ready to generate the token 
      // return this.api.getPheonixToken()
      //   .pipe(
      //     map((response) => {
      //       console.log('Login Serive API Response---', response);
      //       return response;
      //     }, error => {
      //       console.log('Login service - Error Section');
      //     }));

    } else {
      url = this.api.fortressUrl;
      params = {};
      return this.http.post(url, params, { withCredentials: true })
        .pipe(
          map((response) => {
            if (response['access_token'] !== null) {
              token = response['access_token']; // fetch Access token from array.
              localStorage.setItem('headerAuthToken', token);
              this.tokenExpiryTime = response['expires_in'];
            }
            return response;
          }, error => {
            console.log('Login service - Error Section');
          })
        );
    }
  }
}

