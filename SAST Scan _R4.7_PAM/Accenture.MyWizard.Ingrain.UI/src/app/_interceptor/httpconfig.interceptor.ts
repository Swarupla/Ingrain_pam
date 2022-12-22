import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpHeaders
} from '@angular/common/http';

import { Observable, BehaviorSubject } from 'rxjs';
import { ApiService } from '../_services/api.service';
import { LocalStorageService } from '../_services/local-storage.service';

@Injectable()
export class HTTPStatus {
  private requestInFlight$: BehaviorSubject<boolean>;
  constructor() {
    this.requestInFlight$ = new BehaviorSubject(false);
  }

  setHttpStatus(inFlight: boolean) {
    this.requestInFlight$.next(inFlight);
  }

  getHttpStatus(): Observable<boolean> {
    return this.requestInFlight$.asObservable();
  }
}

@Injectable()

export class HttpConfigInterceptor implements HttpInterceptor {
  constructor(public apiService: ApiService, public ls: LocalStorageService) {
  }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

    let token: string = sessionStorage.getItem('headerAuthToken');
    // console.log(token);
    if (request.url.includes('VirtualAssistantAlertMessagesByGroup') || request.url.includes('VirtualAssistantAlertMessagesByCount')) {
      token = sessionStorage.getItem('pheonixToken');
    } else if (request.url.includes('/v1/Account')) {
      token = sessionStorage.getItem('pheonixToken');
    } else if (request.url.includes('/v1/UpdateAccountPrivacy')) {
      token = sessionStorage.getItem('pheonixToken');
    }
    request = request.clone({ url: `${request.url}` });
    // if (request && (request.url === 'https://mywizardapi-devtest-lx.aiam-dh.com/v1/appExecutionContext' ||
    //   request.url === 'https://mywizardapi-devtest-lx.aiam-dh.com/v1/AccountClients')) {

    //   request = request.clone({
    //     headers: new HttpHeaders({
    //       'Cache-Control': 'no-cache',
    //       'Pragma': 'no-cache',
    //       'AppServiceUId': '00040560-0000-0000-0000-000000000000'
    //     }),
    //     setHeaders: {
    //       Authorization: `Bearer ${token ? token : ''}`,

    //       Accept: 'application/json'
    //     }
    //   });
    //   return next.handle(request).pipe();
    // } else {
    /* request = request.clone({
      headers: new HttpHeaders({
        'Cache-Control': 'no-cache',
        'Pragma': 'no-cache',
      }),
      setHeaders: {
        Authorization: `Bearer ${token ? token : ''}`,
        Accept: 'application/json',
      }
    }); */
    if (request.url.includes('VirtualAssistantAlertMessagesByGroup') || request.url.includes('VirtualAssistantAlertMessagesByCount')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache',
        }),
        setHeaders: {
          Authorization: `Bearer ${token ? token : ''}`,
          AppServiceUID: '00040560-0000-0000-0000-000000000000',
          UserEmailId: sessionStorage.getItem('userId'),
          Accept: 'application/json',
        }
      });
    } else if (request.url.includes('/v1/Account')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache',
        }),
        setHeaders: {
          Authorization: `Bearer ${token ? token : ''}`,
          AppServiceUID: '00040560-0000-0000-0000-000000000000',
          UserEmailId: sessionStorage.getItem('userId'),
          Accept: 'application/json',
        }
      });
    } else if ('/v1/GetNonPdfDocByByteArrayForSingleUser') {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache',
        }),
        setHeaders: {
          Authorization: `Bearer ${token ? token : ''}`,
          AppServiceUID: '00040560-0000-0000-0000-000000000000',
          UserEmailId: sessionStorage.getItem('userId')
        }
      });
    } else if (request.url.includes('/v1/UpdateAccountPrivacy')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache',
          'Content-Type': 'application/json'
        }),
        setHeaders: {
          Authorization: `Bearer ${token ? token : ''}`,
          AppServiceUID: '00040560-0000-0000-0000-000000000000',
          CorrelationUID: sessionStorage.getItem('CorrelationUId'),
          Accept: 'application/json',
        }
      });
    } else if (request.url.includes('login.microsoftonline.com')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          // 'Content-Type': 'application/x-www-form-urlencoded'
        })
      });
    } else if (request.url.includes('config.json')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
        })
      });
    } else if (request.url.includes('GeneratePAMToken')) {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
        })
      });
    } else {
      request = request.clone({
        headers: new HttpHeaders({
          'Cache-Control': 'no-cache',
          'Pragma': 'no-cache',
        }),
        setHeaders: {
          Authorization: `Bearer ${token ? token : ''}`,
          Accept: 'application/json',
        }
      });
    }
    return next.handle(request).pipe();

    // }
  }
}
