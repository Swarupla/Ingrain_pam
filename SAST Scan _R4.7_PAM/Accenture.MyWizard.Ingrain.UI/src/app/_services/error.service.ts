import { Injectable, Injector } from '@angular/core';
import { LocationStrategy, PathLocationStrategy } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
import * as StackTraceParser from 'error-stack-parser';

@Injectable(
  { providedIn: 'root' }
)
export class ErrorsService {

  constructor(private injector: Injector) {}

  log(error) {
    const errorToSend = this.addContextInfo(error);
    return of(errorToSend);
  }

  addContextInfo(error) {
    const name = error.name || null;
    const appId = 'shthppnsApp';
    const user = 'ShthppnsUser';
    const time = new Date().getTime();
    const id = `${appId}-${user}-${time}`;
    // tslint:disable-next-line: deprecation
    const location = this.injector.get(LocationStrategy);
    const url = location instanceof PathLocationStrategy ? location.path() : '';
    const status = error.status || null;
    const message = error.message || error.toString();
    const stack = error instanceof HttpErrorResponse ? null : StackTraceParser.parse(error);
    const errorToSend = { name, appId, user, time, id, url, status, message, stack };
    return errorToSend;
  }
}
