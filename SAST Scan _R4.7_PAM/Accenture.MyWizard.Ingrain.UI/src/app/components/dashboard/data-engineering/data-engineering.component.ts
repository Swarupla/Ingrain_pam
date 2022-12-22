import { Component, OnInit, ElementRef, Inject } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { Router } from '@angular/router';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { AppUtilsService } from 'src/app/_services/app-utils.service';

@Component({
  selector: 'app-data-engineering',
  templateUrl: './data-engineering.component.html',
  styleUrls: ['./data-engineering.component.scss']
})
export class DataEngineeringComponent implements OnInit {

  isNextDisabled = false;
  modalRefDataQuality: BsModalRef | null;
  featureNameDataQualityLow = '';
  message = '';

  config = {
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup'
  };

  constructor(private customRouter: CustomRoutingService,
    private router: Router, private ls: LocalStorageService,
    private notificationService: NotificationService,
    private coreUtilsService: CoreUtilsService,
    private _modalService: BsModalService,
    private des: DataEngineeringService,
    private appUtilsService: AppUtilsService) { }

  ngOnInit() {
    this.des.applyFlag$.subscribe(
      bool => {
        this.isNextDisabled = bool;
      }
    );
  }


  next(lessdataquality?) {
    if (!this.coreUtilsService.isNil(this.ls.getFeatureNameDataQualityLow())
      && this.router.url.indexOf('/dashboard/dataengineering/datacleanup') > -1) {
      // console.log( this.ls.getFeatureNameDataQualityLow());
      const data = this.ls.getFeatureNameDataQualityLow();
      if (data === undefined || data === '') {
        this.customRouter.redirectToNext();
      } else {
        this.featureNameDataQualityLow = data.join(' ');
        if (this.featureNameDataQualityLow.includes(this.ls.getTargetColumn())) {
          this.message = 'Attribute ' + this.ls.getTargetColumn() + ' contains less than 50% data quality score ,' +
            'Kindly go back and update the data again';
        } else {
          this.message = 'Attributes ' + this.featureNameDataQualityLow + ' contains less than 50% data quality score ,' +
            'these attributes will not be considered for future processing.';
        }
        this.modalRefDataQuality = this._modalService.show(lessdataquality, this.config);
        // this.notificationService.warning('Some attribute has data quality is less than 50%.');
      }
    } else {
      this.customRouter.redirectToNext();
    }
  }

  confirm() {
    this.modalRefDataQuality.hide();

    if (!this.featureNameDataQualityLow.includes(this.ls.getTargetColumn())) {
      this.appUtilsService.loadingStarted();
      this.des.fixColumns(this.ls.getCorrelationId(), this.ls.getFeatureNameDataQualityLow()).subscribe(data => {
        if (data.substring(0, 7) === 'Success') {
          this.ls.setLocalStorageData('lessdataquality', '');
          this.notificationService.success('Data Fixed Successfully.');
          // localStorage.removeItem('lessdataquality');
          this.appUtilsService.loadingEnded();
          // window.location.reload(); // removed for issue 694218

          this.router.navigate(['/dashboard/dataengineering/dataclaanup'],
            {
              queryParams: {}
            });
          // this.ngOnInit();
        }
        this.appUtilsService.loadingEnded();
      });
    } else if (this.featureNameDataQualityLow.includes(this.ls.getTargetColumn())) {
      //  this.des.setApplyFlag(true);
      this.isNextDisabled = false;
      this.appUtilsService.loadingEnded();
    }
  }

  previous() {
    if (this.customRouter.urlAfterRedirects === '/dashboard/dataengineering/datacleanup'
      || this.router.url === '/dashboard/dataengineering/datacleanup') {
      const modelname = this.ls.getModelName();
      const modelCategory = this.ls.getModelCategory();
      let requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelName=' + modelname + '&pageSource=true';
      if (modelCategory !== null && modelCategory !== undefined && modelCategory !== '') {
        requiredUrl = 'dashboard/problemstatement/usecasedefinition?modelCategory=' + modelCategory +
          '&displayUploadandDataSourceBlock=true&modelName=' + modelname;
      }
      this.customRouter.previousUrl = requiredUrl;
    }
    this.customRouter.redirectToPrevious();
  }
}
