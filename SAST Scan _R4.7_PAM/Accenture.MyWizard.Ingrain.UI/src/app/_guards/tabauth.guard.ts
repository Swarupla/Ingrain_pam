import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { LocalStorageService } from '../_services/local-storage.service';
import { CoreUtilsService } from '../_services/core-utils.service';

@Injectable({
  providedIn: 'root'
})
export class TabauthGuard implements CanActivate {

  constructor(private ls: LocalStorageService, private cu: CoreUtilsService) {
  }
  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {
    const currentUrl = window.location.href;
    if (!this.cu.isNil(currentUrl) &&
      currentUrl.indexOf('fromApp=vds') > -1 && currentUrl.indexOf('CorrelationId=') > -1) {
      return true;
    }
    if ( state.url === '/dashboard/deploymodel/Prediction' || state.url === '/dashboard/deploymodel/Monitoring') { return true;}
    if (this.cu.isNil(this.ls.correlationId) && this.cu.isNil(this.ls.modelName)) {
      return false;
    }

    return true;
  }
}
