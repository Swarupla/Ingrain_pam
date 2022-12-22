import { Component, OnInit, TemplateRef, EventEmitter, Output } from '@angular/core';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { StoreService } from '../../_services/store.service';

import { AppUtilsService } from '../../_services/app-utils.service';
import { ApiService } from '../../_services/api.service';
import { ClientDeliveryStructureService } from '../../_services/client-delivery-structure.service';
import { NotificationService } from '../../_services/notification-service.service';
// import { CookieDeclinePopupComponent } from './cookie-decline-popup/cookie-decline-popup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { Router } from '@angular/router';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent implements OnInit {

  modalRef: BsModalRef | null;
  modalRefTermsOfUse: BsModalRef;
  modalRefCookie: BsModalRef | null;
  accountUId;

  logoutUrl: string;

  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'footer-documentlinks'
  };
  isStyleAvailable: boolean;
  cookiePolicyAccept: boolean = true;
  userId: string;
  userCookie: any;
  payload: any;
  dateOfAcceptance: any;
  instanceName: string;
  environmnet: string;

  @Output() privacyAccepted = new EventEmitter<boolean>();

  constructor(private _bsModalService: BsModalService,
    private storeService: StoreService,
    private appUtilsService: AppUtilsService,
    private clientDelStructService: ClientDeliveryStructureService,
    public router: Router,
    private envService: EnvironmentService) {
    this.storeService.getValue().subscribe((value: boolean) => {
      this.isStyleAvailable = value;
    });
  }

  ngOnInit() {
    this.privacyAccepted.emit(this.cookiePolicyAccept);
    this.userCookie = this.appUtilsService.getCookies();
    this.environmnet = sessionStorage.getItem('Environment');
  }


  openPrivacyPolicy(template: TemplateRef<any>) {
    this.instanceName = sessionStorage.getItem('instanceName');
    this.modalRef = this._bsModalService.show(template, this.config);
    return false;
  }

  openTermsOfUse(templateTermsOfUse: TemplateRef<any>) {
    this.instanceName = sessionStorage.getItem('instanceName');
    this.modalRefTermsOfUse = this._bsModalService.show(templateTermsOfUse, this.config);
    return false;
  }

  openCookiePolicy(templateCookie: TemplateRef<any>) {
    this.instanceName = sessionStorage.getItem('instanceName');
    this.modalRefCookie = this._bsModalService.show(templateCookie, this.config);
    return false;
  }

  acceptCookiePolicy() {
    this.dateOfAcceptance = new Date();
    this.payload = {
      AccountUId: this.accountUId,
      IsPrivacyAccepted: true,
      DateOfAcceptance: this.dateOfAcceptance.toISOString()
    };
    this.clientDelStructService.updateAccountPrivacy(sessionStorage.getItem('clientID'),
      sessionStorage.getItem('dcID'), this.payload).subscribe(
        data => {
          if (data) {
            this.cookiePolicyAccept = true;
            this.privacyAccepted.emit(this.cookiePolicyAccept);
          }
        });
  }

  declineCookiePolicy() {
    sessionStorage.clear();
    localStorage.clear();
    if (this.envService.environment.authProvider === 'AzureAD') {
    }
  }

}
