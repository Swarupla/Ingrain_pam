import { Component, OnInit, ElementRef, Inject, OnDestroy, DoCheck } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { Router, ActivatedRoute } from '@angular/router';
import { CoreUtilsService } from '../../../_services/core-utils.service';
import { LocalStorageService } from './../../../_services/local-storage.service';
import { NotificationService } from '../../../_services/notification-service.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';

@Component({
  selector: 'app-problem-statement',
  templateUrl: './problem-statement.component.html',
  styleUrls: ['./problem-statement.component.scss']
})
export class ProblemStatementComponent implements OnInit, OnDestroy, DoCheck {

  view: Boolean;
  previousDisabled: Boolean = false;
  modelView = false;
  myModelsClass = 'ingrAI-sub-nav-button ingrAI-sub-nav-active';
  useCaseDefClass = 'ingrAI-sub-nav-button hide';
  modelName: string;
  correlationId: string;
  modelCategory: string;
  deliveryConstructUID: string;
  clientUId: string;
  nextAndPreviousButtons = true;
  constructor(@Inject(ElementRef) private eleRef: ElementRef,
    private coreUtilsService: CoreUtilsService, private localStorService: LocalStorageService,
    private customRouter: CustomRoutingService, public router: Router,
    private notificationService: NotificationService, private route: ActivatedRoute,
    public problemStatementService: ProblemStatementService,
    private uts: UsageTrackingService) {

  }

  ngOnInit() {
    /* const divHeader = this.eleRef.nativeElement.parentElement.parentElement.parentElement.querySelector('.client-height');
    if (!this.coreUtilsService.isNil(divHeader)) {
      const showLabel = divHeader.querySelector('#showLablel');
      if (!this.coreUtilsService.isNil(localStorage.getItem('fromApp')) && localStorage.getItem('fromApp') === 'vds') {
        if (!this.coreUtilsService.isNil(showLabel)) {
          showLabel.className = 'col-md-3 enableheader';
        }
        const showDropDown = divHeader.querySelector('#showDropDown');
        if (!this.coreUtilsService.isNil(showDropDown)) {
          showDropDown.className = 'col-md-3 display';
        }
      } else {
        if (!this.coreUtilsService.isNil(showLabel)) {
          showLabel.className = 'col-md-3 display';
        }
        const showDropDown = divHeader.querySelector('#showDropDown');
        if (!this.coreUtilsService.isNil(showDropDown)) {
          showDropDown.className = 'col-md-3 enableheader';
        }
      }
    } */
    this.modelCategory = this.localStorService.getModelCategory();
    this.modelName = this.localStorService.getModelName();
    this.correlationId = this.localStorService.getCorrelationId();
    this.route.queryParams
      .subscribe(params => {
        if (!this.coreUtilsService.isNil(params.clientUId) && !this.coreUtilsService.isNil(params.deliveryConstructUID)) {
          this.clientUId = params.clientUId;
          this.deliveryConstructUID = params.deliveryConstructUID;
        }
      });
    if (!this.coreUtilsService.isNil(this.deliveryConstructUID) && !this.coreUtilsService.isNil(this.clientUId)) {
      this.router.routeReuseStrategy.shouldReuseRoute = () => false;
      //  this.modelView = false;
      //  this.myModelsClass = 'ingrAI-sub-nav-button ingrAI-sub-nav-active';
      //  this.useCaseDefClass = 'ingrAI-sub-nav-button hide';
      if(this.router.url.includes('cascadeModels') != true) {
        this.coreUtilsService.isCascadedModel.redirected = false;
        this.router.navigate(['/dashboard/problemstatement/templates'],
          {
            queryParams: {
              'clientUId': this.clientUId,
              'deliveryConstructUID': this.deliveryConstructUID
            }
          });
      }
    } else if (this.modelCategory === null || this.modelCategory === undefined || this.modelCategory === '') {
      if (this.modelName !== null && this.modelName !== undefined && this.modelName !== '') {
        /* this.modelView = true;
        this.useCaseDefClass = 'ingrAI-sub-nav-button ingrAI-sub-nav-active';
        this.myModelsClass = 'ingrAI-sub-nav-button'; */
        const url = 'dashboard/problemstatement/usecasedefinition?modelName=' + this.modelName + '&pageSource=true';
        if(this.router.url.includes('cascadeModels') != true) {
          this.router.navigateByUrl(url);
        }
      } else if (this.modelName === null || this.modelName === undefined || this.modelName === '') {
        /* this.modelView = false;
        this.myModelsClass = 'ingrAI-sub-nav-button ingrAI-sub-nav-active';
        this.useCaseDefClass = 'ingrAI-sub-nav-button hide'; */
        if(this.router.url.includes('cascadeModels') != true) {
          this.coreUtilsService.isCascadedModel.redirected = false;
          this.router.navigate(['/dashboard/problemstatement/templates']);
        }
      }
    } else {
      if(this.router.url.includes('cascadeModels') != true) {
        this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
          {
            queryParams: {
              'modelCategory': this.modelCategory,
              'displayUploadandDataSourceBlock': true,
              modelName: this.modelName
            }
          });
      }
    }
  }
  ngOnDestroy() {
    /* const divHeader = this.eleRef.nativeElement.parentElement.parentElement.parentElement.querySelector('.client-height');
    if (!this.coreUtilsService.isNil(divHeader)) {
      const showLabel = divHeader.querySelector('#showLablel');
      const showDropDown = divHeader.querySelector('#showDropDown');
      if (!this.coreUtilsService.isNil(showLabel)) {
        showLabel.className = 'col-md-3 enableheader';
      }
      if (!this.coreUtilsService.isNil(showDropDown)) {
        showDropDown.className = 'col-md-3 display';
      }
    } */
  }

  ngDoCheck() {
    /* Aarushi to check for cascadeModels
     if (this.router.url.indexOf('/dashboard/problemstatement/templates') > -1) {
      this.previousDisabled = true;
      this.nextAndPreviousButtons = false;
    } else if (this.router.url.indexOf('cascadeModels') > -1) {
      this.nextAndPreviousButtons = false;
    } else {
      this.previousDisabled = false;
      this.nextAndPreviousButtons = true;
    } */
  }

  /* viewRadio() {
    this.view = true;
  }
  viewMyModel() {
    this.problemStatementService.isPredefinedTemplate = 'False';
    this.coreUtilsService.isCascadedModel.redirected = false;
    if (this.router.url.indexOf('/dashboard/problemstatement/usecasedefinition') > -1) {
      this.localStorService.setLocalStorageData('correlationId', '');
      this.localStorService.setLocalStorageData('modelName', '');
      this.localStorService.setLocalStorageData('modelCategory', '');
      this.modelView = false;
      this.myModelsClass = 'ingrAI-sub-nav-button ingrAI-sub-nav-active';
      this.useCaseDefClass = 'ingrAI-sub-nav-button hide';
      this.uts.usageTracking('Problem Statement', 'My Models');
    }
  }
  next() {
    if (this.coreUtilsService.isNil(localStorage.getItem('isModelSelected'))
      && this.router.url.indexOf('/dashboard/problemstatement/templates') > -1) {
      this.notificationService.warning('Please select a model in my models tab to proceed with its training and deployment.');
    } else if (localStorage.getItem('targetColumn') === 'null') {
      // this.notificationService.warning('Please fill the details and save to proceed with its training and deployment.');
      this.notificationService.warning('Fill the details and save before proceeding to the next page.');
    } else {
      this.customRouter.redirectToNext();
    }
  }

  previous() {
    if (this.router.url.indexOf('/dashboard/problemstatement/usecasedefinition') > -1) {
      this.localStorService.setLocalStorageData('correlationId', '');
      this.localStorService.setLocalStorageData('modelName', '');
      this.localStorService.setLocalStorageData('modelCategory', '');
    }
    this.customRouter.redirectToPrevious();
  }*/
}
