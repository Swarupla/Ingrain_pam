import { Component, OnInit, ViewChild, ElementRef, Inject, OnDestroy, EventEmitter, Output } from '@angular/core';
import { CascadeModelsService } from 'src/app/_services/cascade-models.service';
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

@Component({
  selector: 'app-deploy-cascade-models',
  templateUrl: './deploy-cascade-models.component.html',
  styleUrls: ['./deploy-cascade-models.component.scss']
})
export class DeployCascadeModelsComponent implements OnInit {

  @ViewChild('weblink', { static: false }) weblink;
  @ViewChild('applink', { static: false }) applink;
  applinkshow = false;
  isPopupClosed = false;

  constructor(private _deploymodelService: DeployModelService, private ls: LocalStorageService, private route: ActivatedRoute,
    private router: Router, private _problemStatementService: ProblemStatementService, private apputilService: AppUtilsService,
    private coreUtilsService: CoreUtilsService, private _dialogService: DialogService, private ns: NotificationService,
    private customRouter: CustomRoutingService, private _appUtilsService: AppUtilsService, private uts: UsageTrackingService, 
    private cascadeService: CascadeModelsService) { }

  cascadedId;
  deployedModel;

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
  IsPrivate;
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
  selectedWebLinkIndex: boolean;
  selectedLinkedAppsIndex: number;
  subscription: any;
  paramData: any;
  deliveryTypeName;
  readOnly;
  IsModelTemplate;
  @Output() modelname: EventEmitter<any> = new EventEmitter();

  ngOnInit() {
    this.cascadedId = sessionStorage.getItem('cascadedId');
    this._appUtilsService.loadingStarted();
       this.cascadeService.getCascadeDeployedModel(this.cascadedId).subscribe(response => {
        this._appUtilsService.loadingEnded();
        this.deployedModel = response;
        this.modelname.emit(response.ModelName);
        this.setAttributes(this.deployedModel.CascadeModel[0]);
      }, error => {
        this._appUtilsService.loadingEnded();
        this.ns.error(error.error);
      });
  }

  setAttributes(data) {
  //  this.datasource = data.DataSource;
    this.modelName = data.ModelName;
  //  this.usecase = data.BusinessProblem;
    this.deployedAccuracy = data.Accuracy;
    this.IsPrivate = data.IsPrivate;
    this.modelURL = data.ModelURL;
    this.deployedDate = data.DeployedDate;
    this.status = data.Status;
    this.linkedApps = data.LinkedApps;
    this.inputSmapleForWebLink = data.InputSample;
    this.trainedModelName = data.ModelVersion;
    this.vdsLink = data.VDSLink;
    this.clientUId = data.ClientUId;
    this.deliveryConstructUId = data.DeliveryConstructUID;
    this.IsModelTemplate = data.IsModelTemplate;
    this.getAppLinksDetails(this.linkedApps);
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

  openWebServiceURLWIndow(show) {
    this.selectedWebLinkIndex = show;
    if (this.isPopupClosed) {
    } else {
      this.webserviceLinkshow = true;
    }
    this.isPopupClosed = false;
  }

  closeWebSerciveURLWindow(hide) {
    this.selectedWebLinkIndex = hide;
    this.webserviceLinkshow = false;
    return this.webserviceLinkshow;
  }

  openVdsLink(vdsUrl: string, clientUId: string, deliveryConstructUID: string) {
    const url = vdsUrl + '?clientUId=' + clientUId + '&deliveryConstructUId=' + deliveryConstructUID;
    return url;
  }

  previous() {
    this.router.navigate(['/dashboard/problemstatement/cascadeModels/publishCascadeModels'], {});
  }

}
