import { Component, OnInit, ViewChild, ElementRef, Inject, OnDestroy } from '@angular/core';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ActivatedRoute, Router } from '@angular/router';
import { browserRefresh } from '../../../header/header.component';
import { DeployModelService } from 'src/app/_services/deploy-model.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, switchMap } from 'rxjs/operators';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { empty } from 'rxjs';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { CompletionCertificateComponent } from '../completion-certificate/completion-certificate.component';
import { ApiService } from 'src/app/_services/api.service';

@Component({
  selector: 'app-deployed-model',
  templateUrl: './deployed-model.component.html',
  styleUrls: ['./deployed-model.component.scss']
})
export class DeployedModelComponent implements OnInit, OnDestroy {

  @ViewChild('weblink', { static: false }) weblink;
  @ViewChild('applink', { static: false }) applink;
  applinkshow = false;
  isPopupClosed = false;

  constructor(@Inject(ElementRef) private eleRef: ElementRef,
    private _deploymodelService: DeployModelService, private ls: LocalStorageService, private route: ActivatedRoute,
    private router: Router, private _problemStatementService: ProblemStatementService, private apputilService: AppUtilsService,
    private coreUtilsService: CoreUtilsService, private _dialogService: DialogService, private _notificationService: NotificationService,
    private customRouter: CustomRoutingService, private _appUtilsService: AppUtilsService, private uts: UsageTrackingService,
    private _apiService: ApiService) { }
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
  isMarketPlaceTrialUser: string;
  certificationFlag: boolean;
  deployedModelCount: any;
  showCertificatePopup: any;

  ngOnInit() {
    localStorage.removeItem('modelDeployed');
    this.readOnly = sessionStorage.getItem('viewEditAccess');
    this.correlationId = this.ls.getCorrelationId();
    this.isMarketPlaceTrialUser = localStorage.getItem('marketPlaceTrialUser');
    this.deployedModelCount = localStorage.getItem('DeployedModelCount');
    this.showCertificatePopup = localStorage.getItem('FlagtoShowCertificateModel');
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
      this._deploymodelService.getIsModelDeployed(this.correlationId).subscribe(
        data => {
          this.setServiceAttributes(data);
          this._appUtilsService.loadingEnded();
          if (this.isMarketPlaceTrialUser === 'True') {
            this._problemStatementService.getConfiguredCertificationFlag().subscribe(response => {
              this.certificationFlag = response;
              // console.log(this.certificationFlag);
              if (this.certificationFlag) {
                if (this.deployedModelCount === '0' && this.showCertificatePopup === 'True') {
                  setTimeout(() => {
                    this.openCertificationDialog('test');
                  }, 5000);
                }
              } else if (!this.certificationFlag) {
                if (this.deployedModelCount === '0' && this.showCertificatePopup === 'True') {
                  setTimeout(() => {
                    this.showCongratulationMessage();
                  }, 5000);
                }
              }
            });
          }
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
      this.coreUtilsService.allTabs[1].status = 'disabled';
      this.coreUtilsService.allTabs[2].status = 'disabled';
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

  saveAs() {
    const openTemplateAfterClosed = this._dialogService.open(TemplateNameModalComponent, {}).afterClosed.pipe(
      switchMap(modelName => {
        this.newModelName = modelName;
        if (modelName !== null && modelName !== '' && modelName !== undefined) {
          return this._problemStatementService.clone(this.correlationId, modelName);
        } else {
          // tslint:disable-next-line: deprecation
          return empty();
        }
      }),

      tap(data => this.newCorrelationIdAfterCloned = data),

      tap(data => {
        if (data) {
          this.router.navigate(['dashboard/deploymodel/deployedmodel'], {
            queryParams: { modelName: this.newModelName, correlationId: data }
          });
          this._notificationService.success('Cloned Successfully');
        }
      })
    );
    openTemplateAfterClosed.subscribe();
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
    this.router.navigate(['/dashboard/deploymodel/Monitoring']);
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

  // open certificate/congratulation messages to Marketplace users :start
  openCertificationDialog(abc) {
    const openTemplateAfterClosed = this._dialogService.open(CompletionCertificateComponent, {
      data: {
        configuredCertificationFlag: this.certificationFlag
      }
    }).afterClosed.pipe();
    openTemplateAfterClosed.subscribe();
  }
  showCongratulationMessage() {
    const openMessageTemplateAfterClosed = this._dialogService.open(CompletionCertificateComponent,
      {
        data: {
          configuredCertificationFlag: this.certificationFlag
        }
      }).afterClosed.pipe();
    openMessageTemplateAfterClosed.subscribe();
  }
  // open certificate/congratulation messages to Marketplace users :end
}

