import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, Subject } from 'rxjs';
import { CookieService } from 'ngx-cookie-service';
import { CoreUtilsService } from './core-utils.service';
import { NotificationService } from './notification-service.service';

interface IParamData {
  clientUID: any;
  parentUID: any;
  deliveryConstructUId: any;
  isQueryString: boolean;
  hasMappingInDB: boolean;
}

interface IUserCookie {
  token: string;
  UserId: string;
}
interface IRoleData {
  accessPrivilegeCode: string;
  accessRoleName: string;
}
@Injectable({
  providedIn: 'root'
})


export class AppUtilsService {
  busyCount: any = 0;
  spinnerVisible: any = false;
  private spinnerSubject = new Subject<any>();

  paramData = {} as IParamData;
  userCookie = {} as IUserCookie;
  private apprSubject = new BehaviorSubject<any>('');
  roleData = {} as IRoleData;
  private roleBSubject = new BehaviorSubject<any>('');
  constructor(private cookieService: CookieService, private coreutilServices: CoreUtilsService,
    private _notificationService: NotificationService) {
  }
  setParamData(deliveryConstructUId: any, clientUID: any, parentUID: any, isQueryString: boolean, hasMappingInDB: boolean) {
    if ((this.cookieService.get('Environment') !== 'PAM' || this.cookieService.get('Environment') !== 'FDS') && this.cookieService.get('RequestType') !== 'AM') {
      this.paramData.deliveryConstructUId = this.coreutilServices.isNil(deliveryConstructUId) ? '' : deliveryConstructUId;
      this.paramData.clientUID = this.coreutilServices.isNil(clientUID) ? '' : clientUID;
    } else {
      this.paramData.deliveryConstructUId = this.cookieService.get('DeliveryConstructUId');
      this.paramData.clientUID = this.cookieService.get('ClientUId');
    }
    // this.paramData.deliveryConstructUId = '1e4272c7-d9e6-4df8-b3da-da3d056f35a1';
    // this.paramData.clientUID = '00100000-0000-0000-0000-000000000000'; 
    this.paramData.parentUID = this.coreutilServices.isNil(parentUID) ? '00506000-0000-0000-1111-000000001234' : parentUID;
    this.paramData.isQueryString = isQueryString;
    this.paramData.hasMappingInDB = hasMappingInDB;
    this.apprSubject.next(this.paramData);
  }
  getParamData(): Observable<any> {
    return this.apprSubject.asObservable();
  }
  getCookies() {
    // this.userCookie.token = !this.coreutilServices.isNil(this.cookieService.get('AuthToken'))
    //   ? this.cookieService.get('AuthToken')
    //   : '';
    if (!this.coreutilServices.isNil(sessionStorage.getItem('UserID'))) {
      if ((this.cookieService.get('Environment') !== 'PAM' || this.cookieService.get('Environment') !== 'FDS') && sessionStorage.getItem('RequestType') !== 'AM' && sessionStorage.getItem('RequestType') !== 'IO') {
        this.userCookie.UserId = sessionStorage.getItem('UserID').toLocaleLowerCase();
      } else {
        this.userCookie.UserId = sessionStorage.getItem('UserId').toLocaleLowerCase();
      }
    } else {
      if ((this.cookieService.get('Environment') !== 'PAM' || this.cookieService.get('Environment') !== 'FDS') && sessionStorage.getItem('RequestType') !== 'AM' && sessionStorage.getItem('RequestType') !== 'IO') {
        this._notificationService.error('User Id is null');
        // this.userCookie.UserId = 'mywizardsystemdataadmin@mwphoenix.onmicrosoft.com';
        // this.userCookie.UserId = 'bala.b.krishnan@ds.dev.accenture.com'; 
        // this.userCookie.UserId = 'siddesha.k.s@ds.dev.accenture.com';
        //  this.userCookie.UserId = 'bala.b.krishnan@accenture.com';
      } else {
        this.userCookie.UserId = sessionStorage.getItem('UserId').toLocaleLowerCase();
      }
      // 'pravin.chandankhede@mywizard.com';// 'admin@mywizard.com'; 'hariom.thakur@accenture.com' //;
    }
    return this.userCookie;
  }
  deleteCookies() {
    this.userCookie.token = '';
    this.userCookie.UserId = '';
    this.cookieService.deleteAll();
    sessionStorage.clear();
    localStorage.clear();
  }
  loadingStarted() {
    this.busyCount++;
    if (!this.spinnerVisible) {
      this.spinnerVisible = true;
      this.spinnerSubject.next(this.spinnerVisible);
    }
  }

  loadingEnded() {
    this.busyCount--;
    this.busyCount = (this.busyCount < 0) ? 0 : this.busyCount;
    if (this.busyCount === 0) {
      this.spinnerVisible = false;
      this.spinnerSubject.next(this.spinnerVisible);
    }
  }
  getSpinnerSubject() {
    return this.spinnerSubject;
  }
  setRoleData(role: any) {
    this.roleData.accessPrivilegeCode = role.AccessPrivilegeCode;
    this.roleData.accessRoleName = role.AccessRoleName;
    this.roleBSubject.next(this.roleData);
  }
  getRoleData(): Observable<any> {
    return this.roleBSubject.asObservable();
  }

  // Added loadingImmediateEnded For loading Issue in retry calling runtest api
  loadingImmediateEnded() {
    this.busyCount = 0;
    // this.busyCount = (this.busyCount < 0) ? 0 : this.busyCount;
    if (this.busyCount === 0) {
      this.spinnerVisible = false;
      this.spinnerSubject.next(this.spinnerVisible);
    }
  }
}
