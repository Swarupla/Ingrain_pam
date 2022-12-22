import { Component, OnInit, OnDestroy, ElementRef, Inject, ViewChild } from '@angular/core';
import { ProblemStatementService } from '../../../../_services/problem-statement.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { UploadFileComponent } from 'src/app/components/upload-file/upload-file.component';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { Router, ActivatedRoute } from '@angular/router';
import { tap } from 'rxjs/operators';
import { FileUploadProgressBarComponent } from 'src/app/components/file-upload-progress-bar/file-upload-progress-bar.component';
import { Subscription } from 'rxjs';
import { AppUtilsService } from '../../../../_services/app-utils.service';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import { RegressionPopupComponent } from '../regression-popup/regression-popup.component';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { ModelDeletionPopupComponent } from './model-deletion-popup/model-deletion-popup.component';
import { TruncatePublicModelnamePipe } from '../../../../_pipes/truncate-public-modelname.pipe';
import { DatePipe } from '@angular/common';
import { browserRefreshforApp } from '../../../root/app.component';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ArchiveModelService } from './archived-model-list/archive-model.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-pst-templates',
  templateUrl: './pst-templates.component.html',
  styleUrls: ['./pst-templates.component.scss'],
  providers: [TruncatePublicModelnamePipe]
})

export class PstTemplatesComponent implements OnInit, OnDestroy {
  public browserRefresh: boolean;
  displayModelGroupContent = false;

  templatesCategories;
  templates;
  templateModelNames;
  selectedIndex;
  searchText;
  userId: any;
  date;
  models;
  modelArr;
  Myfiles: any;
  columnData: string;
  modelName: any;
  correlationId: any;
  subscription: Subscription;
  paramData: any;
  totalRecords: number;
  totalRec: number;
  page = 1;
  clientUId: string;
  deliveryConstructUID: string;
  templateClass: string;
  myModelClass: string;
  linkTemplateClass: string;
  linkMyModelClass: string;
  truncatedModelNames = [];
  truncatedmodelcorrelationid = {};
  isPopupClosed = false;
  webserviceLinkshow = false;
  selectedWebLinkIndex: number;
  isPrivate: string;
  itemsPerPage = 10;
  toggle = true;
  modalRef: BsModalRef | null;
  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup',
    message: ''
  };
  @ViewChild('myModelArchivalToggle', { static: false }) myModelArchivalToggle: any;
  myModelSelected = true;
  // disablePAToggle = false;
  archivedModelSelected = false;
  archivedModelList;
  ismodelMovedToMymodel : boolean = false;

  constructor(private route: ActivatedRoute, @Inject(ElementRef) private eleRef: ElementRef,
    private problemStatementService: ProblemStatementService, private coreUtilsService: CoreUtilsService,
    private apputilService: AppUtilsService, private ls: LocalStorageService,
    private dialogService: DialogService, private router: Router,
    private truncateString: TruncatePublicModelnamePipe, private datePipe: DatePipe,
    private _modalService: BsModalService, private _archiveModelService: ArchiveModelService,
    private ns : NotificationService) {
  }

  ngOnInit() {
    localStorage.setItem('featureMapping', 'False');
    this.myModelClass = 'tab-pane fade active show';
    this.linkMyModelClass = 'nav-link active show';
    this.userId = this.apputilService.getCookies().UserId;
    this.subscription = this.apputilService.getParamData().subscribe(paramData => {

      this.paramData = paramData;
      this.clientUId = paramData.clientUID ? paramData.clientUID : sessionStorage.getItem('ClientUId');
      this.deliveryConstructUID = paramData.deliveryConstructUId ? paramData.deliveryConstructUId : sessionStorage.getItem('DeliveryConstructUId');
      this.browserRefresh = browserRefreshforApp;
      if (this.browserRefresh) {
        if (localStorage.getItem('marketPlaceRedirected') === 'True') {
          this.navigateToUseCaseDefinition();
        } else if (localStorage.getItem('marketPlaceTrialUser') === 'True' && this.ls.getMPCorrelationid() !== undefined) {
          this.navigateToTrialTemplates();
        } else if (this.myModelSelected) {
          this.getTempate();
        }
      }
    });


    this.route.queryParams
      .subscribe(params => {
        if (!this.coreUtilsService.isNil(params.clientUId) && !this.coreUtilsService.isNil(params.deliveryConstructUID)) {
          this.clientUId = params.clientUId;
          this.deliveryConstructUID = params.deliveryConstructUID;
          this.displayModelGroupContent = false;
          this.templateClass = 'tab-pane fade';
          this.myModelClass = 'tab-pane fade show active';
          this.linkTemplateClass = 'nav-link';
          this.linkMyModelClass = 'nav-link active show';

        }
      });
    if (localStorage.getItem('marketPlaceRedirected') === 'True') {
      this.navigateToUseCaseDefinition();
    } else if (localStorage.getItem('marketPlaceTrialUser') === 'True' && this.ls.getMPCorrelationid() !== undefined) {
      this.navigateToTrialTemplates();
    } else if (!this.coreUtilsService.isNil(this.deliveryConstructUID) && !this.coreUtilsService.isNil(this.clientUId)) {
      if (this.myModelSelected) {
        this.getTempate();
      }
    }
    localStorage.removeItem('isModelSelected');

  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  getTempate() {
    const me = this;
    this.problemStatementService.getPublicTemplatess(true,
      this.userId, 'release Management', null, this.deliveryConstructUID, this.clientUId).subscribe(
        data => {
          me.templates = data;
          me.templatesCategories = JSON.parse(this.templates.Categories).Category;
        }
      );
    const today = new Date();
    this.date = this.formatDate(today);
    const _this = this;
    this.apputilService.loadingStarted();
    this.problemStatementService.getModels(false, this.userId, null, this.date, this.deliveryConstructUID, this.clientUId).subscribe(
      data => {
        me.models = data.filter(
          models => models.ClientUId === this.clientUId && models.DeliveryConstructUID === this.deliveryConstructUID);
        me.models.reverse();
        me.totalRec = me.models.length;
        this.modelArr = me.models;
        this.totalRecords = me.totalRec;

        const ele = _this.eleRef.nativeElement.parentElement;
        const myModelEle = !this.coreUtilsService.isNil(ele) ? ele.querySelector('#myModel') : ele;
        if (!this.coreUtilsService.isNil(myModelEle) && myModelEle.className.indexOf(' active') > -1) {
          this.ls.setLocalStorageData('correlationId', '');
          this.ls.setLocalStorageData('modelName', '');
          this.ls.setLocalStorageData('modelCategory', '');
        }
        this.apputilService.loadingEnded();
      }, error => {
        this.apputilService.loadingEnded();
      }
    );
  }
  setCorrelationIdinLocalStorage(correlationId, viewEditAccess?) {
    // this.ls.setLocalStorageData('correlationId', '1cb11b06-fffa-48cf-8e9c-35feb98f6c3e');
    this.ls.setLocalStorageData('correlationId', correlationId);
    localStorage.setItem('isModelSelected', 'true');
    sessionStorage.removeItem('viewEditAccess');
    if (viewEditAccess === true) {
      viewEditAccess = 'true';
      sessionStorage.setItem('viewEditAccess', viewEditAccess);
    }
  }

  setModelNameinLocalStorage(modelName, ModelVersion) {
    this.ls.setLocalStorageData('modelName', modelName);
    sessionStorage.setItem('modelVersion', ModelVersion);
  }

  onClickCategory(templatesCategory, index: number) {
    this.truncatedModelNames = [];
    const value = templatesCategory;
    this.displayModelGroupContent = true;
    this.selectedIndex = index;
    if (value) {
      this.problemStatementService.getPublicTemplatess(true, this.userId, value, null, this.deliveryConstructUID, this.clientUId).subscribe(
        data => {
          this.templates = data;
          this.templateModelNames = this.templates.publicTemplates;
          this.templateModelNames.forEach(i => {
            this.truncatedModelNames.push(this.truncateString.transform(i.ModelName));
            this.truncatedmodelcorrelationid[this.truncateString.transform(i.ModelName)] = [(i.CorrelationId), templatesCategory];
          });
        }
      );
    }
  }

  onSelectedModel(name) {
    const correlationId = this.truncatedmodelcorrelationid[name][0];
    const tempCategory = this.truncatedmodelcorrelationid[name][1];
    const displayUploadandDataSourceBlock = true;
    const tempModelName = name;

    this.ls.setLocalStorageData('correlationId', correlationId);
    this.ls.setLocalStorageData('modelName', tempModelName);
    this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
      {
        queryParams: {
          'modelCategory': tempCategory,
          'displayUploadandDataSourceBlock': displayUploadandDataSourceBlock,
          modelName: tempModelName
        }
      });
  }

  selectMyModels() {
    this.displayModelGroupContent = false;
  }

  openModalPopUp() {
    const fileUploadAfterClosed = this.dialogService.open(UploadFileComponent, {}).afterClosed
      .pipe(
        tap(filesData => filesData ? this.openModalNameDialog(filesData) : '')
      );

    fileUploadAfterClosed.subscribe();
  }

  openModalNameDialog(filesData) {
    this.Myfiles = filesData;

    const openTemplateAfterClosed =
      this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name' } }).afterClosed.pipe(
        tap(modelName => modelName ? this.openFileProgressDialog(filesData, modelName) : '')
      );

    openTemplateAfterClosed.subscribe();
  }

  openFileProgressDialog(filesData, modelName) {
    this.modelName = modelName;
    const openFileProgressAfterClosed = this.dialogService.open(FileUploadProgressBarComponent,
      { data: { filesData: filesData, modelName: modelName } }).afterClosed.pipe(
        tap(data => data ? this.navigateToUseCaseDefinition(data.body[0]) : '')
      );
    openFileProgressAfterClosed.subscribe();
  }

  navigateToUseCaseDefinition(correlationId?) {
    if (localStorage.getItem('marketPlaceRedirected') === 'True') {
      // this.navigateMPUserToUseCaseDefination(this.modelCategory, this.modelName);	
      this.problemStatementService.isPredefinedTemplate = 'True';
      this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
        {
          queryParams: {
            'modelCategory': this.ls.getMPModelCategory(),
            'displayUploadandDataSourceBlock': true,
            modelName: this.ls.getMPModelName(),
            'isMPuser': true
          }
        });
    } else {
      this.correlationId = correlationId;
      this.ls.setLocalStorageData('correlationId', correlationId);
      this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
        {
          queryParams: {
            'modelCategory': '',
            'displayUploadandDataSourceBlock': false,
            'modelName': this.modelName
          }
        });
    }
  }

  navigateToTrialTemplates() {
    if (localStorage.getItem('marketPlaceTrialUser') === 'True') {
      this.problemStatementService.isPredefinedTemplate = 'True';
      this.router.navigate(['/dashboard/problemstatement/usecasedefinition'],
        {
          queryParams: {
            'modelCategory': this.ls.getMPModelCategory(),
            'displayUploadandDataSourceBlock': true,
            modelName: this.ls.getMPModelName(),
            'isMPuser': true,
            'source': 'tryNow'
          }
        });
    }
  }

  deleteModel(correlationId: string, modelName: string, cascadeModel: boolean) {
    const openModelDeletionPopup = this.dialogService.open(ModelDeletionPopupComponent,
      { data: { correlationId: correlationId, modelName: modelName, isCascadeModel: cascadeModel } })
      .afterClosed.pipe(
        tap(isDataFlushFlag => isDataFlushFlag ? this.deleteModelByCorrelationId(correlationId) : '')
      );
    openModelDeletionPopup.subscribe();
  }

  deleteModelByCorrelationId(correlationId: string) {
    if (!this.coreUtilsService.isNil(correlationId)) {
      this.apputilService.loadingStarted();
      this.problemStatementService.deleteModelByCorrelationId(correlationId).subscribe(
        data => {
          this.ngOnInit();
          this.apputilService.loadingEnded();
        }
      );
    }
  }

  formatDate(date) {
    //const priorDate = this.addMonths(date, -5);
    const priorDate = this.addMonths(date, -6);// Changes done as per requirement.

    return this.datePipe.transform(priorDate, 'yyyy-MM-dd');
  }

  addMonths(date, months) {
    date.setMonth(date.getMonth() + months);
    return date;
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
  searchModel(searchText) {
    this.models = this.modelArr;
    this.totalRec = this.totalRecords;
    if (!this.coreUtilsService.isNil(searchText)) {

      this.isPrivate = '';
      const privateTxt = 'private';
      const publicTxt = 'public';
      if (!this.coreUtilsService.isNumeric(searchText) && privateTxt.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
        this.isPrivate = 'true';
      }
      if (!this.coreUtilsService.isNumeric(searchText) && publicTxt.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
        this.isPrivate = 'false';
      }
      this.models = this.modelArr.filter((item) => {
        return (!this.coreUtilsService.isNil(item.ModelName) && item.ModelName.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.DeployedDate) && item.DeployedDate.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.ModelVersion) && item.ModelVersion.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.Status) && item.Status.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(this.isPrivate) && item.Status !== 'In Progress'
            && item.IsPrivate.toString().indexOf(this.isPrivate) > -1)
          || (!this.coreUtilsService.isNil(item.Accuracy) && item.Accuracy.toString().indexOf(searchText) > -1);
      });
      this.totalRec = this.models.length;
    }
  }

  navigateToPredictionTab(model) {
    this.setCorrelationIdinLocalStorage(model.CorrelationId);
    this.setModelNameinLocalStorage(model.ModelName, model.ModelVersion)
    this.router.navigate(['/dashboard/deploymodel/Monitoring']);
  }

  navigateToCascadeModels(model?) {
    if (model) {
      sessionStorage.setItem('cascadedId', model.CorrelationId);
      sessionStorage.setItem('cascadedCategory', model.Category);
    }
    if (model) {
      if (model.Status == 'Deployed') {
        this.router.navigate(['/dashboard/problemstatement/cascadeModels/mapCascadeModels'],
          {
            queryParams: {
              'modelName': this.modelName,
              'cascadedId': model.CorrelationId
            }
          });
      } else {
        this.router.navigate(['dashboard/problemstatement/cascadeModels/chooseCascadeModels'],
          {
            queryParams: { 'cascadedId': model.CorrelationId, 'category': model.Category }
          });
      }
    } else {
      this.router.navigate(['dashboard/problemstatement/cascadeModels/chooseCascadeModels'],
        {
          queryParams: {}
        });
    }
  }

  clearSearch() {
    this.models = this.modelArr;
    this.searchText = '';
  }

  FMModelInProgressPopup(FMInProgressPopup, status) {
    let message = '';
    if (status === 'E') {
      message = 'Some error occurred while model training. Please try training another model.';
    } else {
      message = 'Model training is in-progress, please try after sometime';
    }
    this.config = {
      backdrop: true,
      ignoreBackdropClick: true,
      class: 'deploymodle-confirmpopup',
      message: message
    };
    this.modalRef = this._modalService.show(FMInProgressPopup, this.config);
  }

  switchMyModelArchival(event) {
    if (event.currentTarget.checked === true) {
      this.archivedModelSelected = true;
      this.myModelSelected = false;
      this.displayArchivedModel();
    } else {
      this.ismodelMovedToMymodel = false;
      this.archivedModelSelected = false;
      this.myModelSelected = true;
      this.getTempate();
    }
  }

  displayArchivedModel() {
   // let archivalDays = 6; // value 6 is in months
    this.apputilService.loadingStarted();
    this._archiveModelService.getArchivalModelList(this.userId,this.deliveryConstructUID,this.clientUId).subscribe(response => {
      if (response) {
        this.archivedModelList = response;
        if(this.ismodelMovedToMymodel === true){
          this.ns.success('Model moved to My Model section.');
        }
        console.log('ArchivalDays,,', response);
      }
      this.apputilService.loadingEnded();
    }, error => {
      this.apputilService.loadingEnded();
    });
  }

  archivedModelMovedToMyModel($event) {
    if ($event !== undefined) {
      // this.archivedModelSelected = false;
      // this.myModelSelected = true;
      // if (this.myModelArchivalToggle) {
      //   this.myModelArchivalToggle.nativeElement.checked = false;
      // }
      this.getTempate();
      this.displayArchivedModel();
      this.ismodelMovedToMymodel = true;
    }
  }
}
