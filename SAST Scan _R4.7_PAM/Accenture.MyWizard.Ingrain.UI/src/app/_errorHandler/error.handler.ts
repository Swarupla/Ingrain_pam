import { ErrorHandler, Injectable, Injector } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { NotificationService } from '../_services/notification-service.service';
import { ErrorsService } from '../_services/error.service';
import { Router } from '@angular/router';


@Injectable({
  providedIn: 'root'
})

export class ErrorsHandler implements ErrorHandler {
  constructor(private injector: Injector) {
  }
  handleError(error) {
    const ns = this.injector.get(NotificationService);
    const errorsService = this.injector.get(ErrorsService);
    const router = this.injector.get(Router);

    if (error instanceof HttpErrorResponse) {
      if (!navigator.onLine) {
        return ns.error('No Internet Connection');
      }
      if (error.status === 500) {
        ns.error('Server is not responding, Please try after some time.');
      }
      if (error.status === 401) {
        ns.error('Unauthorized, Please Login again');
      }
      if (error.status === 403) {
        ns.error('You are not Allowed to Access this Page');
      }
      if (error.status === 404) {
        ns.error('The server can not find requested resource');
      }
      if (error.status === 408) {
        ns.error('Request Timeout, server is So Slow, May be your network issue');
      }
      errorsService.log(error).subscribe();
      return error;

    } else {
      errorsService.log(error)
        .subscribe(errorWithContext => {
          if (errorWithContext.message.includes('no_account_error')) {
            console.error(errorWithContext.message);
          } else {
            ns.error(errorWithContext.message, errorWithContext.name);
          }
        });
    }
  }
}
