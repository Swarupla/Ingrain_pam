import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { CascadeModelsService } from 'src/app/_services/cascade-models.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap } from 'rxjs/operators';
import { Router, ActivatedRoute } from '@angular/router';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';

@Component({
  selector: 'app-choose-cascade-models',
  templateUrl: './choose-cascade-models.component.html',
  styleUrls: ['./choose-cascade-models.component.scss']
})
export class ChooseCascadeModelsComponent implements OnInit {

  constructor(private customRouter: CustomRoutingService, private coreUtilService: CoreUtilsService,
    private cascadeService: CascadeModelsService, private ns: NotificationService,
    private appUtilsService: AppUtilsService, private dialogService: DialogService,
    private router: Router, private route: ActivatedRoute, private ls: LocalStorageService,
    private _modalService: BsModalService, private pb: ProblemStatementService) {
    this.appUtilsService.getParamData().subscribe(paramData => {
      this.paramData = paramData;
    });

  }

  isIE: boolean;
  modelNumber = 2;
  deliveryType = 'AD';
  correlationId = null;
  paramData;
  modelNames = [];
  cascadedId = null;
  minModel = 2;
  maxModel = 5;
  /* modelList = {'Model1': {'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null},
  'Model2': {'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null},
  'Model3': {'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null},
  'Model4': {'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null},
  'Model5': {'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null}
  }; */
  newModel = { 'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null, 'ModelType': null, 'ApplicationID': null, 'LinkedApps': null, 'TargetColumn': null };
  modelList = {
    'Model1': { 'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null, 'ModelType': null, 'ApplicationID': null, 'LinkedApps': null, 'TargetColumn': null },
    'Model2': { 'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null, 'ModelType': null, 'ApplicationID': null, 'LinkedApps': null, 'TargetColumn': null }
  };
  modelName = null;
  modalRef: BsModalRef | null;
  config = {
    ignoreBackdropClick: true,
    class: 'ingrAI-create-model ingrAI-enter-model'
  };
  totalModels = 2;
  masterData;
  objectKeys = Object.keys;
  @Output() modelname: EventEmitter<any> = new EventEmitter();
  @Output() cascadedid: EventEmitter<any> = new EventEmitter();
  @Output() deliverytype: EventEmitter<any> = new EventEmitter();
  modelStatus = 'New';
  isModelUpdated = false;
  removedModels = [];
  isCustomModel = false;
  env;
  requestType;

  ngOnInit() {
    this.isIE = this.coreUtilService.isIE();
    this.env = sessionStorage.getItem('Environment');
    this.requestType = sessionStorage.getItem('RequestType');
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
      this.deliveryType = 'AIops';
    }
    this.route.queryParams.subscribe(params => {
      if (params.cascadedId) {
        this.cascadedId = params.cascadedId;
        sessionStorage.setItem('cascadedId', this.cascadedId);
        this.cascadedid.emit(this.cascadedId);
        this.deliveryType = params.category;
        this.deliverytype.emit(this.deliveryType);
      }
      this.getAllModels();
    });
  }

  getAllModels() {
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
      this.deliveryType = this.requestType;
    }
    const userId = sessionStorage.getItem('userId');
    this.appUtilsService.loadingStarted();
    let dcId = this.paramData.deliveryConstructUId;
    let clientId = this.paramData.clientUID;
    if (this.coreUtilService.isNil(dcId)) {
      dcId = sessionStorage.getItem('dcID');
    }
    if (this.coreUtilService.isNil(clientId)) {
      clientId = sessionStorage.getItem('clientID');
    }
    this.cascadeService.getCascadingModels(clientId, dcId,
      userId, this.deliveryType, this.cascadedId).subscribe(data => {
        this.appUtilsService.loadingEnded();
        this.masterData = data;
        this.modelNames = data.Models;
        this.isCustomModel = data.IsCustomModel;
        this.deliveryType = data.Category;
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
          //  if(this.deliveryType === 'AM' || this.deliveryType === 'IO') {
          this.deliveryType = 'AIops';
          // }
        }
        this.deliverytype.emit(this.deliveryType);
        if (data.ModelsList !== null) {
          this.modelList = JSON.parse(JSON.stringify(data.ModelsList)); // creating a deep copy of modelList
          this.removeAlreadySelectedModels();
        }
        this.modelName = data.ModelName;
        this.modelname.emit(this.modelName);
        this.totalModels = Object.keys(this.modelList).length;
        this.modelNumber = this.totalModels;
        this.modelStatus = data.Status;
      },
        error => {
          this.appUtilsService.loadingEnded();
          this.ns.error(error.error.ErrorMessage);
        });
  }

  removeAlreadySelectedModels() {
    Object.entries(this.modelList).forEach(
      ([key, value]) => {
        if (this.modelNames.length > 0) {
          this.modelNames.splice(this.modelNames.findIndex(e => e.CorrelationId === value['CorrelationId']), 1);
        }
      });
  }

  next(modelNamePopup, modelChangesConfirmation) {
    if (this.modelList['Model1'].ModelName !== null && this.modelList['Model' + this.totalModels].ModelName !== null) {
      if (this.coreUtilService.isNil(this.modelName)) {
        this.showModelEnterDialog(modelNamePopup);
      } else {
        if (!this.coreUtilService.isNil(this.modelList)) {
          this.checkModelSequence(modelChangesConfirmation);
        } else {
          this.saveChosenModels(this.modelName);
        }
      }
    } else {
      this.ns.error('Kindly drop model name in Start and End tabs');
    }
  }

  onDrag(event, columnName) {
    if (this.isIE) {
      event.dataTransfer.setData('Text', 'value#' + columnName.ModelName);
      event.dataTransfer.setData('correlationId', 'id#' + columnName.CorrelationId);
    } else {
      event.dataTransfer.setData('value', columnName.ModelName);
      event.dataTransfer.setData('correlationId', columnName.CorrelationId);
    }
  }

  onDrop(event, index) {
    let value;
    let correlationId;
    if (this.isIE) {
      const text = event.dataTransfer.getData('Text');
      const splitvalues = text.split('#');
      value = splitvalues[1];
      const id = event.dataTransfer.getData('correlationId');
      const idSplitValue = text.split('#');
      correlationId = idSplitValue[1];
    } else {
      value = event.dataTransfer.getData('value');
      correlationId = event.dataTransfer.getData('correlationId');
    }
    /*  this.modelList['Model' + index].ModelName = value;
     this.modelList['Model' + index].CorrelationId = correlationId; */
    if (this.modelList['Model' + index]['ModelName'] !== null) {
      this.modelNames.push(this.modelList['Model' + index]);
      // To remove duplicate entries in modelNames
      let textColumn = new Set(this.modelNames);
      this.modelNames = Array.from(textColumn);
    }
    const currentModel = this.modelNames.filter(e => e.CorrelationId === correlationId);
    /* this.modelList['Model' + index].ProblemType = currentModel[0].ProblemType;
    this.modelList['Model' + index].Accuracy = currentModel[0].Accuracy; */
    if (currentModel.length > 0) {
      const newModel = { 'CorrelationId': correlationId, 'ModelName': value, 'ProblemType': currentModel[0].ProblemType, 'Accuracy': currentModel[0].Accuracy, 'ModelType': currentModel[0].ModelType, 'ApplicationID': currentModel[0].ApplicationID, 'LinkedApps': currentModel[0].LinkedApps, 'TargetColumn': currentModel[0].TargetColumn };
      this.modelList['Model' + index] = newModel;
      if (this.modelNames.length > 0) {
        this.modelNames.splice(this.modelNames.findIndex(e => e.CorrelationId === correlationId), 1);
      }
    }
  }

  allowDrop(event) {
    event.preventDefault();
  }

  removeColumn(correlationId, index, removeColumn?) {
    const _this = this;
    if (correlationId === null) {
      delete this.modelList['Model' + index];
      let newModelList;
      newModelList = {};
      Object.entries(this.modelList).forEach(
        ([key, value], i) => {
          newModelList['Model' + (i + 1)] = value;
        }
      );
      this.modelList = newModelList;
    } else {
      const modelId = Object.keys(this.modelList).find(key => this.modelList[key].CorrelationId === correlationId);
      const removedColumn = this.modelList[modelId];
      if (this.modelList[modelId]['ModelName'] !== null) {
        _this.modelNames.push(removedColumn);
      }
      if (removeColumn === undefined) {
        this.modelList[modelId] = this.newModel;
      } else {
        delete this.modelList[modelId];
        let newModelList;
        newModelList = {};
        Object.entries(this.modelList).forEach(
          ([key, value], i) => {
            newModelList['Model' + (i + 1)] = value;
          }
        );
        this.modelList = newModelList;
      }
    }
    this.totalModels = Object.keys(this.modelList).length;
  }

  addIntermediateModel() {
    if (this.modelNumber >= this.minModel && this.modelNumber < this.maxModel) {
      const existingValue = this.modelList['Model' + this.modelNumber];
      this.modelList['Model' + this.modelNumber] = this.newModel;
      this.modelNumber++;
      this.modelList['Model' + this.modelNumber] = existingValue;
      this.totalModels = Object.keys(this.modelList).length;
    }
  }

  removeIntermediateModel(correlationId, index) {
    if (this.modelNumber > this.minModel && this.modelNumber <= this.maxModel) {
      this.modelNumber--;
      const removeColumn = true;
      this.removeColumn(correlationId, index, removeColumn);
    }
  }

  onDeliveryChange(deliveryType) {
    this.modelList = {
      'Model1': { 'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null, 'ModelType': null, 'ApplicationID': null, 'LinkedApps': null, 'TargetColumn': null },
      'Model2': { 'CorrelationId': null, 'ModelName': null, 'ProblemType': null, 'Accuracy': null, 'ModelType': null, 'ApplicationID': null, 'LinkedApps': null, 'TargetColumn': null }
    };
    this.deliveryType = deliveryType;
    this.deliverytype.emit(this.deliveryType);
    this.modelNumber = 2;
    this.getAllModels();
  }

  /* Enter Model Name Dialog */
  showModelEnterDialog(modelNamePopup) {
    /* const openTemplateAfterClosed =
    this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name' } }).afterClosed.pipe(
        tap(modelName => modelName ? this.saveChosenModels(modelName) : '')
    );
    openTemplateAfterClosed.subscribe(); */
    this.modalRef = this._modalService.show(modelNamePopup, this.config);
  }

  saveChosenModels(modelName) {
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
      this.deliveryType = this.requestType;
    }
    this.modelName = modelName;
    this.modelname.emit(this.modelName);
    if (this.cascadedId === undefined) {
      this.cascadedId = null;
      this.cascadedid.emit(this.cascadedId);
    }
    const payload = {
      'ModelName': this.modelName,
      'ClientUId': this.paramData.clientUID,
      'CascadedId': this.cascadedId,
      'DeliveryConstructUID': this.paramData.deliveryConstructUId,
      'Category': this.deliveryType,
      'ModelList': this.modelList,
      'CreatedByUser': this.appUtilsService.getCookies().UserId,
      'isModelUpdated': this.isModelUpdated,
      'RemovedModels': this.removedModels
    };
    this.appUtilsService.loadingStarted();
    this.cascadeService.saveCascadeModels(payload).subscribe(data => {
      this.appUtilsService.loadingEnded();
      this.cascadedId = data.CascadedId;
      sessionStorage.setItem('cascadedId', this.cascadedId);
      this.cascadedid.emit(this.cascadedId);
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
        //  if(this.deliveryType === 'AM' || this.deliveryType === 'IO') {
        this.deliveryType = 'AIops';
        // }
      }
      //  this.customRouter.redirectToNext();
      this.router.navigate(['/dashboard/problemstatement/cascadeModels/mapCascadeModels'],
        {
          queryParams: {
            'modelName': this.modelName,
            'cascadedId': this.cascadedId
          }
        });
    }, error => {
      this.appUtilsService.loadingEnded();
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType == 'AM' || this.requestType == 'IO')) {
        //  if(this.deliveryType === 'AM' || this.deliveryType === 'IO') {
        this.deliveryType = 'AIops';
        // }
      }
      this.ns.error(error.error);
    });
  }

  onEnter(modelInput) {
    this.modalRef.hide();
    const valid = this.coreUtilService.isSpecialCharacter(this.modelName);
    if (valid === 0) {
      return 0;
    } else {
      this.modelName = modelInput.value;
      if (modelInput.valid) {
        this.appUtilsService.loadingStarted();
        this.pb.getExistingModelName(modelInput.value).subscribe(message => {
          if (message === false) {
            //   this.setModelNameinLocalStorage(this.modalName);
            this.saveChosenModels(modelInput.value);

            this.appUtilsService.loadingImmediateEnded();
          } else {
            message = 'The model name already exists. Choose a different name.';
            this.ns.error(message);
            this.appUtilsService.loadingImmediateEnded();
          }
        }, error => {
          this.modalRef.hide();
          this.appUtilsService.loadingImmediateEnded();
          this.ns.error('Something went wrong.');
        });
      }
      if (modelInput.invalid && modelInput.touched) {
        this.ns.error('Enter Model Name');
      }
      if (modelInput.pristine) {
        this.ns.error('Enter Model Name');
      }
    }
  }

  onClose() {
    this.modelName = null;
    this.modalRef.hide();
  }

  checkModelSequence(modelChangesConfirmation) {
    /* if (JSON.stringify(this.masterData['modelList']) !== JSON.stringify(this.modelList)) {
      this.modalRef = this._modalService.show(modelChangesConfirmation, this.config);
    } else {
      this.saveChosenModels(this.modelName);
    } */
    let equalObjects = true;
    Object.entries(this.modelList).forEach(
      ([key, value]) => {
        if (this.masterData['ModelsList'].hasOwnProperty(key)) {
          if (this.masterData['ModelsList'][key]['CorrelationId'] !== value['CorrelationId']) {
            equalObjects = false;
          }
        } else {
          equalObjects = false;
        }
      });
    if (equalObjects === true) {
      this.saveChosenModels(this.modelName);
    } else {
      this.modalRef = this._modalService.show(modelChangesConfirmation, this.config);
    }
  }

  onConfirm() {
    this.modalRef.hide();
    this.isModelUpdated = true;
    let modelListArray = [];
    let masterDataArray = [];
    Object.entries(this.modelList).forEach(
      ([key, value]) => {
        modelListArray.push(value['CorrelationId']);
      });
    Object.entries(this.masterData['ModelsList']).forEach(
      ([key, value]) => {
        masterDataArray.push(value['CorrelationId']);
      });
    masterDataArray.forEach(element => {
      if (modelListArray.includes(element) === false) {
        this.removedModels.push(element);
      }
    });
    this.saveChosenModels(this.modelName);
  }

}
