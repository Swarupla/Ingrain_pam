import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpResponse, HttpHeaders } from '@angular/common/http';
import { from, Observable, of } from 'rxjs';
import { Injectable, Injector, Inject } from '@angular/core';
import { mergeMap, switchMap, tap } from 'rxjs/operators';
import 'rxjs/add/operator/mergeMap';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from '../msal-authentication/msal-authentication.service';
import { Token } from '../_models/Token';

@Injectable()
export class AuthorizationInterceptor implements HttpInterceptor {

  constructor(private envService: EnvironmentService, private msalAuthentication: AuthenticationService) {
  }

  public static lastCalled: Date = null;

  intercept(request: HttpRequest<any>, next: HttpHandler):
    Observable<HttpEvent<any>> {
    AuthorizationInterceptor.lastCalled = new Date();
    const url = request.urlWithParams;

    if (url.startsWith('.') || url.startsWith('/')) {
      return next.handle(request)
        .pipe(tap((event: HttpEvent<any>) => {
          //if the event is for http response
          if (event instanceof HttpResponse) {
            // stop our loader here
          }
        }
        ));
    }
    const formToken = localStorage.getItem('headerAuthToken');
    let pheonixToken = sessionStorage.getItem('pheonixToken');
    // sessionStorage.setItem("lastCalled", new Date().toLocaleString());
    if (request.url.includes('VirtualAssistantAlertMessagesByGroup')) {
      pheonixToken = sessionStorage.getItem('pheonixToken');
    } else if (request.url.includes('/v1/Account')) {
      pheonixToken = sessionStorage.getItem('pheonixToken');
    } else if (request.url.includes('/v1/UpdateAccountPrivacy')) {
      pheonixToken = sessionStorage.getItem('pheonixToken');
    } else if (request.url.includes('/v1/ForcedSignIn')) {
      pheonixToken = sessionStorage.getItem('pheonixToken');
    }
    request = request.clone({ url: `${request.url}` });

    if (this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase() ||
      this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase()) {
      if (request.url.includes('VirtualAssistantAlertMessagesByGroup')
        || request.url.includes('VirtualAssistantAlertMessagesByCount')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
          }),
          setHeaders: {
            Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
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
            Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
            AppServiceUID: '00040560-0000-0000-0000-000000000000',
            UserEmailId: sessionStorage.getItem('userId'),
            Accept: 'application/json',
            username: sessionStorage.getItem('userId').split('@')[0]
          }
        });
      } else if (request.url.includes('/v1/GetNonPdfDocByByteArrayForSingleUser')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
          }),
          setHeaders: {
            Authorization: `Bearer ${formToken ? formToken : ''}`,
            AppServiceUID: '00040560-0000-0000-0000-000000000000',
            UserEmailId: sessionStorage.getItem('userId')
          }
        });
      } else if (request.url.includes('/v1/VideoPlayer')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
          }),
          setHeaders: {
            Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
            // AppServiceUID: '00040560-0000-0000-0000-000000000000',
            // UserEmailId: sessionStorage.getItem('userId'),
            // Accept: 'application/json',
          }
        });
      } else if (request.url.includes('/v1/UserNotificationMessages')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
            // Accept: 'application/json'                    
          }),
          setHeaders: {
            Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
            Accept: 'application/json',
            appServiceUId: this.envService.environment.AppServiceUID
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
            Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
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
            Authorization: `Bearer ${formToken ? formToken : ''}`,
            Accept: 'application/json',
          }
        });
      }
      return next.handle(request).pipe();
      // if (!request.headers.has('Authorization')) {
      //     request = request.clone({
      //         setHeaders: {
      //             Authorization: `Bearer ${sessionStorage.getItem('accessToken')}`
      //         }, url: url
      //     });
      //     // console.log("Request", request);
      // }
      // return next.handle(request)
      //     .pipe(tap((event: HttpEvent<any>) => {
      //         // if the event is for http response
      //         if (event instanceof HttpResponse) {
      //         }
      //     }));
      // }
    } else if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      const resourceUrl = this.envService.environment.resourceId;
      return this.msalAuthentication.getToken()
        .pipe(switchMap((token: Token) => {
          if (token != null) {
            // clone and modify the request
            if (request.url.includes('VirtualAssistantAlertMessagesByGroup')
              || request.url.includes('VirtualAssistantAlertMessagesByCount')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                  'Pragma': 'no-cache',
                }),
                setHeaders: {
                  Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
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
                  Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
                  AppServiceUID: '00040560-0000-0000-0000-000000000000',
                  UserEmailId: sessionStorage.getItem('userId'),
                  Accept: 'application/json',
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
                  Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
                  AppServiceUID: '00040560-0000-0000-0000-000000000000',
                  CorrelationUID: sessionStorage.getItem('CorrelationUId'),
                  Accept: 'application/json',
                }
              });
            } else if (request.url.includes('config.json')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                })
              });
            } else if (request.url.includes('/v1/ForcedSignIn')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                  'Pragma': 'no-cache',
                  'Content-Type': 'application/json'
                }),
                setHeaders: {
                  Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
                  AppServiceUID: '00040560-0000-0000-0000-000000000000',
                  CorrelationUID: sessionStorage.getItem('CorrelationUId'),
                  Accept: 'application/json',
                }
              });
            } else if (request.url.includes('GeneratePAMToken')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                })
              });
            } else if (request.url.includes('/v1/Account')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                  'Pragma': 'no-cache',
                }),
                setHeaders: {
                  Authorization: 'Bearer ' + token['access_token'],
                  AppServiceUID: '00040560-0000-0000-0000-000000000000',
                  Accept: 'application/json',
                }
              });
            }else if (request.url.includes('/v1/UserNotificationMessages')) {
              request = request.clone({
                headers: new HttpHeaders({
                  'Cache-Control': 'no-cache',
                  'Pragma': 'no-cache',
                }),
                setHeaders: {
                  Authorization: `Bearer ${pheonixToken ? pheonixToken : ''}`,
                  Accept: 'application/json',
                  appServiceUId: this.envService.environment.AppServiceUID
                }
              });
            } else {
              request = request.clone({
                setHeaders: {
                  Authorization: 'Bearer ' + token['access_token'],
                  Accept: 'application/json',
                }, url: url
              });
            }
          }
          return next.handle(request);
        }));
      
    } else if (this.envService.environment.authProvider.toLowerCase() === 'WindowsAuthProvider'.toLowerCase()) {
      if (request.url.includes('VirtualAssistantAlertMessagesByGroup')
        || request.url.includes('VirtualAssistantAlertMessagesByCount')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
          }),
          setHeaders: {
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
            AppServiceUID: '00040560-0000-0000-0000-000000000000',
            UserEmailId: sessionStorage.getItem('userId'),
            Accept: 'application/json',
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
            AppServiceUID: '00040560-0000-0000-0000-000000000000',
            CorrelationUID: sessionStorage.getItem('CorrelationUId'),
            Accept: 'application/json',
          }
        });
      } else if (request.url.includes('config.json')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
          })
        });
      } else if (request.url.includes('/v1/ForcedSignIn')) {
        request = request.clone({
          headers: new HttpHeaders({
            'Cache-Control': 'no-cache',
            'Pragma': 'no-cache',
            'Content-Type': 'application/json'
          }),
          setHeaders: {
            AppServiceUID: '00040560-0000-0000-0000-000000000000',
            CorrelationUID: sessionStorage.getItem('CorrelationUId'),
            Accept: 'application/json',
          }
        });
      } else {
        request = request.clone({
          setHeaders: {
            Accept: 'application/json',
          },
          withCredentials: true, url: url
        });
        return next.handle(request);
      }
    }
  }
}
