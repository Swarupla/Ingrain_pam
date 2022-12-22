import { Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AdDeployModelService } from 'src/app/_services/ad-deploy-model.service';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';

@Component({
  selector: 'app-ad-deployed-model',
  templateUrl: './ad-deployed-model.component.html',
  styleUrls: ['./ad-deployed-model.component.scss']
})
export class AdDeployedModelComponent implements OnInit, OnDestroy {

    @ViewChild('weblink', { static: false }) weblink;
    @ViewChild('applink', { static: false }) applink;
    applinkshow = false;
    isPopupClosed = false;
  
    constructor(@Inject(ElementRef) private eleRef: ElementRef,
      private _deploymodelService: AdDeployModelService, private ls: LocalStorageService, private route: ActivatedRoute,
      private router: Router,private coreUtilsService: CoreUtilsService, private _notificationService: NotificationService,
      private _appUtilsService: AppUtilsService, private uts: UsageTrackingService, private _apiService: ApiService) { }
    modelName: String;
    datasource;
    usecase;
  
    userId: String;
    correlationId;
    modelsData;
    modelNames = [];
    trainedModels = [];
    searchText;
    newModelName: string;
    newCorrelationIdAfterCloned;
    deployedAccuracy;
    IsPrivate: string;
    modelURL: string;
    deployedDate;
    status: string;
    linkedApps: [];
    linkedAppsLength: number;
    linkedAppsName: string[] = [];
    webserviceLinkshow = false;
    inputSmapleForWebLink: string;
    trainedModelName: string;
    vdsLink: string;
    clientUId: string;
    deliveryConstructUId: string;
    problemType: string;
    isTimeSeries = false;
    arrDeployModel: string[];
    selectedWebLinkIndex: number;
    selectedLinkedAppsIndex: number;
    subscription: any;
    paramData: any;
    deliveryTypeName;
    readOnly;
    deployedModelCount: any;
    currentIndex = 2;
    breadcrumbIndex = 1;
  
    ngOnInit() {
      localStorage.removeItem('modelDeployed');
      this.readOnly = sessionStorage.getItem('viewEditAccess');
      this.correlationId = this.ls.getCorrelationId();
      this.deployedModelCount = localStorage.getItem('DeployedModelCount');
      this._appUtilsService.loadingStarted();
      this.uts.usageTracking('Deploy Model', 'Deployed model');
      const ele = this.eleRef.nativeElement.parentElement;
      const myModelEle = !this.coreUtilsService.isNil(ele) ? ele.querySelector('#btnNext') : ele;
      if (!this.coreUtilsService.isNil(myModelEle) && myModelEle.className.indexOf('btn btn-primary btn-next') > -1) {
        myModelEle.className = 'hide-nxt-btn';
      }
      if (this.route.snapshot.queryParams['Accuracy']) {
        this.route.queryParams
          .subscribe(params => {
            this.setAttributes(params);
            this._appUtilsService.loadingEnded();
          }, error => {
            this._appUtilsService.loadingEnded();
          });
      } else {
        this._deploymodelService.getIsModelDeployedAD(this.correlationId).subscribe(
          data => {
            this.setServiceAttributes(data);
            this._appUtilsService.loadingEnded();
          }, error => {
            this._appUtilsService.loadingEnded();
            this._notificationService.error(`Error occurred: Due
            to some backend data process
             the relevant data could not be produced.
              Please try again while we troubleshoot the error`);
          });
      }
      this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {
        this.paramData = paramData;
      });
    }
    ngOnDestroy() {
      const ele = this.eleRef.nativeElement.parentElement;
      const myModelEle = !this.coreUtilsService.isNil(ele) ? ele.querySelector('#btnNext') : ele;
      if (!this.coreUtilsService.isNil(myModelEle) && myModelEle.className.indexOf('hide-nxt-btn') > -1) {
        myModelEle.className = 'btn btn-primary btn-next';
      }
    }
    setAttributes(data) {
      this.datasource = data.DataSource;
      this.modelName = data.ModelName;
      this.usecase = data.BusinessProblem;
      this.deployedAccuracy = data.Accuracy;
  
      if (data.IsPrivate === 'true') {
        this.IsPrivate = 'Private';
      } else if (data.IsPrivate === 'false') {
        this.IsPrivate = 'Public';
      }
      this.modelURL = data.ModelURL;
      this.deployedDate = data.DeployedDate;
      this.status = data.Status;
      this.linkedApps = data.LinkedApps;
      this.inputSmapleForWebLink = data.InputSample;
      this.trainedModelName = data.ModelVersion;
      this.vdsLink = data.VDSLink;
      this.clientUId = data.ClientUId;
      this.deliveryConstructUId = data.DeliveryConstructUID;
      this.getAppLinksDetails(this.linkedApps);
    }
  
    setServiceAttributes(data) {
      this.datasource = data.DataSource;
      this.modelName = data.ModelName;
      this.usecase = data.BusinessProblem;
      this.problemType = data.ModelType;
      this.arrDeployModel = data.DeployModels as string[];
      this.deliveryTypeName = data.Category;
      if (data.DeployModels?.length > 0 && data.DeployModels[0]['IsFMModel'] === true) {
        this.coreUtilsService.allADTabs[1].status = 'disabled';
        this.coreUtilsService.allADTabs[2].status = 'disabled';
      }
      for (let i = 0; i < data.DeployModels.length; i++) {
        this.linkedApps = data.DeployModels[i].LinkedApps;
        this.getAppLinksDetails(this.linkedApps);
      }
    }
  
    setTimeSeriesAttributes(data) {
      this.datasource = data.DataSource;
      this.modelName = data.ModelName;
      this.usecase = data.BusinessProblem;
      this.problemType = data.ModelType;
      this.deliveryTypeName = data.Category;
      if (this.problemType === 'TimeSeries') {
        this.isTimeSeries = true;
        this.arrDeployModel = data.DeployModels as string[];
      }
    }
  
    getAppLinksDetails(apps: []) {
      this.linkedAppsName = [];
      if (!this.coreUtilsService.isNil(apps)) {
        for (let l = 0; l < apps.length; l++) {
          if (apps[l] === '') {
            this.linkedAppsLength = 0;
          } else {
            this.linkedAppsLength = apps.length;
            this.linkedAppsName.push(apps[l]);
          }
        }
        this.linkedAppsName = this.linkedAppsName.map(Function.prototype.call, String.prototype.trim);
      }
    }
  
    openAppDetails(index: number) {
      this.selectedLinkedAppsIndex = index;
      if (this.isPopupClosed) {
  
      } else {
        if (this.linkedAppsLength === 0) {
          this.applinkshow = false;
        } else {
          this.applinkshow = true;
        }
      }
      this.isPopupClosed = false;
    }
  
    closeAppDetails(index: number) {
      this.selectedLinkedAppsIndex = index;
      this.applinkshow = false;
      return this.applinkshow;
    }
  
    openWebServiceURLWIndow(index: number) {
      this.selectedWebLinkIndex = index;
      if (this.isPopupClosed) {
      } else {
        this.webserviceLinkshow = true;
      }
      this.isPopupClosed = false;
    }
  
    closeWebSerciveURLWindow(index: number) {
      this.selectedWebLinkIndex = index;
      this.webserviceLinkshow = false;
      return this.webserviceLinkshow;
    }
  
    saveSelectedData() {
    }
  
    openVdsLink(vdsUrl: string, clientUId: string, deliveryConstructUID: string) {
      if(this._apiService.ingrainappUrl.includes('saas')) {
      const url = vdsUrl + '?E2EUId=' + sessionStorage.getItem('End2EndId') + '&Instance=PAM';
      return url;
      } else {
        const url = vdsUrl + '?clientUId=' + clientUId + '&deliveryConstructUId=' + deliveryConstructUID;
        return url;
      }
    }
  
    navigateToPredictionTab(model) {
      this.setCorrelationIdinLocalStorage(model.CorrelationId);
      this.setModelNameinLocalStorage(model.ModelName, model.ModelVersion)
      this.router.navigate(['/anomaly-detection/deploymodel/Monitoring']);
    }
  
    setModelNameinLocalStorage(modelName, ModelVersion) {
      this.ls.setLocalStorageData('modelName', modelName);
      sessionStorage.setItem('modelVersion', ModelVersion);
    }
  
    setCorrelationIdinLocalStorage(correlationId) {
      // this.ls.setLocalStorageData('correlationId', '1cb11b06-fffa-48cf-8e9c-35feb98f6c3e');
      this.ls.setLocalStorageData('correlationId', correlationId);
      localStorage.setItem('isModelSelected', 'true');
    }
  
  }
