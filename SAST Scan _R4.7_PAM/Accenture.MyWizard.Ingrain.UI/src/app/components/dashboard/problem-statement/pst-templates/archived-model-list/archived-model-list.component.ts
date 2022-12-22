import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Subscription } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { ArchiveModelService } from './archive-model.service';

@Component({
  selector: 'app-archived-model-list',
  templateUrl: './archived-model-list.component.html',
  styleUrls: ['./archived-model-list.component.css']
})
export class ArchivedModelListComponent implements OnInit {

  @Input() archivedModelList;
  @Output() moveToMyModelSection = new EventEmitter<string>();

  listOfArchivedModels = [];
  subscription: Subscription;
  paramData: any;
  totalRecords: number;
  totalRec: number;
  page = 1;
  clientUId: string;
  deliveryConstructUID: string;
  userId: string;
  itemsPerPage = 10;
  modalRef: BsModalRef | null;
  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup',
    message: ''
  };
  selectedWebLinkIndex: number;
  webserviceLinkshow = false;
  toggle : boolean = true;
  searchText : string;
  isPrivate : string;
  modelArr;
  

  constructor(private apputilService: AppUtilsService, private _archiveModelService: ArchiveModelService,
    private _localStorageService: LocalStorageService, private _modalService: BsModalService,private router: Router,
     private coreUtilsService: CoreUtilsService) { }

  ngOnInit(): void {
    localStorage.setItem('myModelsScreen', 'False');
    this.userId = this.apputilService.getCookies().UserId;
    this.subscription = this.apputilService.getParamData().subscribe(paramData => {
      this.paramData = paramData;
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });

  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.archivedModelList?.currentValue != '' && changes.archivedModelList?.currentValue !== undefined) {
      let deployedArchiveModelList = changes.archivedModelList.currentValue;
      deployedArchiveModelList = _.without(deployedArchiveModelList, undefined);

      this.listOfArchivedModels = deployedArchiveModelList.filter(
        archiveModels => archiveModels.ClientUId === this.clientUId && archiveModels.DeliveryConstructUID === this.deliveryConstructUID);
      this.totalRec = this.listOfArchivedModels.length;
      this.modelArr = this.listOfArchivedModels;
      this.totalRecords = this.totalRec;
      this.searchModel(this.searchText);
    }
  }

  moveToMyModel(correlationId) {
    this.apputilService.loadingStarted();
    this._archiveModelService.retrivePurgedModel(correlationId).subscribe(response => {
      this.apputilService.loadingEnded();
      this.moveToMyModelSection.emit(correlationId);
    }, error => {
      this.apputilService.loadingEnded();
    });
  }


  /* check if model navigation and prediction navigation we can do */

  setCorrelationIdinLocalStorage(correlationId, viewEditAccess?) {    
    this._localStorageService.setLocalStorageData('correlationId', correlationId);
    localStorage.setItem('isModelSelected', 'true');
    sessionStorage.removeItem('viewEditAccess');
    if (viewEditAccess === true) {
      viewEditAccess = 'true';
      sessionStorage.setItem('viewEditAccess', viewEditAccess);
    }
  }

  setModelNameinLocalStorage(modelName, ModelVersion) {
    this._localStorageService.setLocalStorageData('modelName', modelName);
    sessionStorage.setItem('modelVersion', ModelVersion);
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
              'modelName': model.ModelName,
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

  closeWebSerciveURLWindow(index: number) {
    this.selectedWebLinkIndex = index;
    this.webserviceLinkshow = false;
    return this.webserviceLinkshow;
  }

  searchModel(searchText) {
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
      this.listOfArchivedModels = this.modelArr.filter((item) => {
        return (!this.coreUtilsService.isNil(item.ModelName) && item.ModelName.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.DeployedDate) && item.DeployedDate.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.ModelVersion) && item.ModelVersion.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(item.Status) && item.Status.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
          || (!this.coreUtilsService.isNil(this.isPrivate) && item.Status !== 'In Progress'
            && item.IsPrivate.toString().indexOf(this.isPrivate) > -1)
          || (!this.coreUtilsService.isNil(item.Accuracy) && item.Accuracy.toString().indexOf(searchText) > -1);
      });
      this.totalRec = this.listOfArchivedModels.length;
    }
  }

  clearSearch() {
    this.listOfArchivedModels = this.modelArr;
    this.searchText = '';
  }

}
