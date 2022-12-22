import { Component, OnInit, ElementRef, TemplateRef } from '@angular/core';
import { tap, switchMap } from 'rxjs/operators';
import { RegressionPopupComponent } from 'src/app/components/dashboard/problem-statement/regression-popup/regression-popup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ClientDeliveryStructureService } from '../../../_services/client-delivery-structure.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ApiService } from 'src/app/_services/api.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
  selector: 'app-user-help',
  templateUrl: './user-help.component.html',
  styleUrls: ['./user-help.component.scss'],
  host: {
    '(document:click)': 'onClick($event)'
  }
})


export class UserHelpComponent implements OnInit {
  show: boolean;
  modalRef: BsModalRef | null;
  modalRefTermsOfUse: BsModalRef;
  modalRefAboutUs: BsModalRef;
  releaseversion: string;
  assemblyversion: string;
  releasedate: string;
  clientID: string;
  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'footer-documentlinks'
  };
  openToggle = false;
  instanceName: string;
  environment;
  disableAccentureLinks : boolean;

  constructor(private _eref: ElementRef, private dialogService: DialogService,
    private _bsModalService: BsModalService, private clientDelStructService: ClientDeliveryStructureService,
    private ns: NotificationService, private appUtilsService: AppUtilsService, private _apiService: ApiService,
    private envService : EnvironmentService) { }


  ngOnInit() {

    if (sessionStorage.getItem('Environment') === 'FDS' || sessionStorage.getItem('Environment') === 'PAM') {
      this.environment = sessionStorage.getItem('Environment');
    }
    this.instanceName = sessionStorage.getItem('instanceName');
    this.appUtilsService.getParamData().subscribe(paramdata => {
      this.clientID = paramdata.clientUID;
    });
    if (this.clientID !== undefined && this.clientID !== null) {
      
      if (sessionStorage.getItem('Environment') !== 'FDS' || sessionStorage.getItem('Environment') !== 'PAM') {

        this.clientDelStructService.getAboutUsReleaseDetail(this.clientID,
          sessionStorage.getItem('userId')).subscribe(
            data => {
              if (data) {
                this.releaseversion = data['ReleaseVersion'];
                this.assemblyversion = data['AssemblyVersion'];
                this.releasedate = data['ReleaseDate'];
              }
            }, error => {
              // this.ns.error('Something went wrong.');
            });
      }
    }
    this.disableAccentureLinks = this.envService.environment.disableAccentureLinks;
  }

  openRegressionPopup() {
    const openFileProgressAfterClosed = this.dialogService.open(RegressionPopupComponent,
      { data: {} }).afterClosed.pipe(
        tap(data => '')
      );
    openFileProgressAfterClosed.subscribe();
  }

  onClick(event) {
    if (!this._eref.nativeElement.contains(event.target)) {
      this.show = false;
    } else {
      if (document.getElementsByClassName('nav-active').length) {
        this.show = false;
        if ( event.target.id === 'demoVideoId') {
          this.show = true;
        }
      } else {
        // this.show = true;
        this.show = (event.target.id === 'user-guide' || event.target.id === 'terms-condition') ? false : true;
      }

    }
  }

  openTermsOfUse(templateTermsOfUse: TemplateRef<any>) {
    this.instanceName = sessionStorage.getItem('instanceName');
    this.modalRefTermsOfUse = this._bsModalService.show(templateTermsOfUse, this.config);
  }

  openaboutusPopup(templateaboutus: TemplateRef<any>) {
    this.instanceName = sessionStorage.getItem('instanceName');
    this.modalRefAboutUs = this._bsModalService.show(templateaboutus, this.config);
  }

  navigateToVideoSection() {
    const mywizardHomeUrl = this._apiService.mywizardHomeUrl;
    const myWizardIngrainUrl = this._apiService.ingrainAPIURL;
    let VideoUrl = (sessionStorage.getItem('Environment') === 'FDS') ?  myWizardIngrainUrl.substring(myWizardIngrainUrl.lastIndexOf('ing'),myWizardIngrainUrl.indexOf('/api')) + '/video' : 'ingrain/video';//myWizardIngrainUrl.substring(url.lastIndexOf('ing'),url.indexOf('/api'))
    console.log(VideoUrl);
    const url =  mywizardHomeUrl.slice(0, mywizardHomeUrl.length - 4) + VideoUrl;
    window.open(url);
    // window.open('http://localhost:4200/video'); // using for local
   }

}
