import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';


interface IParamData {
  clientUID: any;
  parentUID: any;
  deliveryConstructUId: any;
}

@Component({
  selector: 'app-ad-upload-data',
  templateUrl: './ad-upload-data.component.html',
  styleUrls: ['./ad-upload-data.component.scss']
})
export class AdUploadDataComponent implements OnInit {

  @ViewChild('fileInput', { static: false }) fileInput: any;
  @Output() uploadedData = new EventEmitter<any>();

  paramData = {} as IParamData;
  sourceTotalLength: number;
  files = [];
  uploadFilesCount: number;
  allowedExtensions = ['.xlsx', '.csv'];
  env: string;
  selectedParentFile: any = [];
  entityArray = [];
  deliveryTypeList: Array<string>;
  fileView : boolean = true;
  entityView : boolean = false;
  entityFormGroup: FormGroup;
  entityList = [];
  requestType: string;
  entityLoader = true;
  loaderScreenIE: boolean = false;
  entityselection: boolean = false;
  entityList2 = [];
  allEntities = [];
  agileFlag: boolean;
  removalEntityFlag: boolean;
  selectedParentType: any;
  selectedDeliveryType: string;
  selectedEntity;
  apiRow: FormGroup;
  language: string;

  constructor(private _appUtilsService : AppUtilsService, private ns : NotificationService,
    private formBuilder: FormBuilder, private problemStatementService : ProblemStatementService,
    private coreUtilService : CoreUtilsService) {
      if (localStorage.getItem('CustomFlag')) {
        this.agileFlag = true;
      } else {
        this.agileFlag = false;
      }
     }

  ngOnInit(): void {
    this.env = sessionStorage.getItem('Environment');
    this.requestType = sessionStorage.getItem('RequestType');
    this._appUtilsService.getParamData().subscribe(data => this.paramData = data);
    this.apiRow = this.formBuilder.group({
      //  apiSelect: [''],
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      subEntityControl: [''],
    });
    let entityArrayLength = this.entityArray.length;
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      entityArrayLength = 0;
    }
    this.sourceTotalLength = this.files.length + entityArrayLength;
    if (sessionStorage.getItem('Language')) {
      this.language = sessionStorage.getItem('Language').toLowerCase();
  } else {
      this.language = 'english';
  }
  }

  onChangeOFEntity() {
    const entityValue = this.entityFormGroup.get('entityControl').value;
    this.entityselection = true;
    let entityValueLength = entityValue.length;
    let entityArrayLength = 0;
    if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
      entityValueLength = 1;
    }
    const sourceLength = this.files.length + entityValueLength;
    if (sourceLength <= 5) {
      if (this.entityFormGroup.get('startDateControl').status === 'VALID' &&
        this.entityFormGroup.get('endDateControl').status === 'VALID') {
        this.entityArray = this.entityFormGroup.get('entityControl').value;
        if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
          let selectedEntity = [];
          selectedEntity.push(this.entityFormGroup.get('entityControl').value);
          this.entityArray = selectedEntity;
          entityArrayLength = this.entityArray.length;
        } else {
          entityArrayLength = this.entityArray.length;
        }

        if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
          this.entityArray[0] = this.entityFormGroup.get('entityControl').value;
          if (this.removalEntityFlag) {
            entityArrayLength = 0;
            this.removalEntityFlag = false;
          } else {
            entityArrayLength = 1;
          }
        }
        this.sourceTotalLength = this.files.length + entityArrayLength;
      }
    } else {
      this.ns.error('Maximum 5 sources of data allowed.');
      this.entityFormGroup.get('entityControl').setValue(this.entityArray);
    }
  }

  onChangeOfDeliveryType(deliveryName) {
    const deliveryTypeValue = this.entityFormGroup.get('deliveryTypeControl').value;
    let deliveryTypeValueLength = deliveryTypeValue.length;
    deliveryTypeValueLength = 1;

    this.entityList = this.allEntities.filter(x => {
      const deliveryTypes = x.sub.split('/');
      if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
        if (deliveryName === 'PPM' && x.name.indexOf('Iteration') > -1) {
          return false;
        } else {
          return x;
        }
      }
    });

    this.entityFormGroup.get('startDateControl').enable();
    this.entityFormGroup.get('endDateControl').enable();
  }

  setFileView() {
    this.fileView = true;
    this.entityView = false;
    this.entityselection = false;
    this.entityFormGroup = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });
    this.entityList = [];
    this.entityArray = [];
  }

  setEntityView() {
    this.fileView = false;
    this.entityView = true;
    this.entityArray = [];
    this.entityFormGroup = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });

    if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
      this.deliveryTypeList = ['AIops'];
    } else {
      this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others', 'PPM'];
    }

    if (this.env === 'FDS' && this.requestType === 'AM') {
      this.entityLoader = false;
      this.entityList2 = [{ 'name': 'Incidents', 'sub': 'ALL' },
      { 'name': 'ServiceRequests', 'sub': 'ALL' },
      { 'name': 'ProblemTickets', 'sub': 'ALL' },
      { 'name': 'WorkRequests', 'sub': 'ALL' },
      { 'name': 'CHANGE_REQUEST', 'sub': 'ALL' }]; // 
    } else if (this.env === 'FDS' && this.requestType === 'IO') {
      this.entityLoader = false;
      this.entityList2 = [{ 'name': 'Incidents', 'sub': 'ALL' },
      { 'name': 'ServiceRequests', 'sub': 'ALL' },
      { 'name': 'ProblemTickets', 'sub': 'ALL' },
      { 'name': 'ChangeRequests', 'sub': 'ALL' }];
    }
    else{
      this.entityLoader = true;
      this.getEntityDetails();
    }
  }

  
  removeEntity(index: number, entityName: string) {
    // if ((this.env !== 'PAM' || this.env !== 'FDS') && this.requestType !== 'AM' && this.requestType !== 'IO') {
    //     this.entityArray = this.entityArray.filter(e => e !== entityName);
    // }
    let entityArrayLength = this.entityArray.length;
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        entityArrayLength = 0;
        this.removalEntityFlag = true;
        this.entityArray = [];
        this.apiRow.get('entityControl').setValue(this.entityArray);
    }
    this.sourceTotalLength = this.files.length + entityArrayLength;
    this.apiRow.get('entityControl').setValue(this.entityArray);
}

  public setParentFile(selectedParentName, type) {
    this.selectedParentFile[0] = selectedParentName;
    this.selectedParentType = type;
  }

  getEntityDetails() {
    this.entityList = [];
    this.allEntities = [];
    this.entityArray = [];
    this.problemStatementService.getDynamicEntity(this.paramData.clientUID, this.paramData.deliveryConstructUId, this._appUtilsService.getCookies().UserId).subscribe(
      data => {
        this.entityLoader = false;
        let entitiesArray;
        Object.entries(data).forEach(
          ([key, value]) => {
            entitiesArray = value;
            entitiesArray.forEach(entity => {
              const obj = { 'name': entity, 'sub': key };
              this.allEntities.push(obj);
            });
          });

        if (this.entityList.length === 0) {
          this.entityFormGroup.get('startDateControl').disable();
          this.entityFormGroup.get('endDateControl').disable();
        }
      },
      error => {
        this.entityLoader = false;
        this.ns.error('Pheonix metrics api has no data.');
      });
  }

  getFileDetails(e) {
    this._appUtilsService.loadingStarted();
    if (this.agileFlag === true) {
      this.uploadFilesCount = e.target.files.length;
    } else {
      this.uploadFilesCount += e.target.files.length;
    }
    let validFileExtensionFlag = true;
    let validFileNameFlag = true;
    let validFileSize = true;
    const resourceCount = e.target.files.length;
    if (this.sourceTotalLength <= 1 && resourceCount <= (1 - this.sourceTotalLength)) {
      const files = e.target.files;
      let fileSize = 0;
      for (let i = 0; i < e.target.files.length; i++) {
        const fileName = files[i].name;
        const dots = fileName.split('.');
        const fileType = '.' + dots[dots.length - 1];
        if (!fileName) {
          validFileNameFlag = false;
          this._appUtilsService.loadingImmediateEnded();
          break;
        }
        if (this.allowedExtensions.indexOf(fileType) !== -1) {
          fileSize = fileSize + e.target.files[i].size;
          const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
          validFileNameFlag = true;
          validFileExtensionFlag = true;
          if (index < 0) {
            if (this.agileFlag === true) {
              this.files = [];
              this.files.push(e.target.files[0]);
            } else {
              this.files.push(e.target.files[i]);
            }
            this.uploadFilesCount = this.files.length;
            this.sourceTotalLength = this.files.length + this.entityArray.length;
            let entityArrayLength = this.entityArray.length;
            if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
              if (this.removalEntityFlag) {
                entityArrayLength = 0;
              } else {
                if (this.entityselection) {
                  entityArrayLength = 1;
                } else {
                  entityArrayLength = 0;
                }
              }
              this.sourceTotalLength = this.files.length + entityArrayLength;
            } else {
              this.sourceTotalLength = this.files.length + this.entityArray.length;
            }
          }
          this._appUtilsService.loadingImmediateEnded();
        } else {
          validFileExtensionFlag = false;
          break;
        }
      }
      if (fileSize <= 136356582) {
        validFileSize = true;
      } else {
        validFileSize = false;
        this.files = [];
        this.uploadFilesCount = this.files.length;
        this.sourceTotalLength = this.files.length + this.entityArray.length;
      }
      if (validFileNameFlag === false) {
        this.ns.error('Kindly upload a file with valid name.');
      }
      if (validFileExtensionFlag === false) {
        this.ns.error('Kindly upload .xlsx or .csv file.');
      }
      if (validFileSize === false) {
        this.ns.error('Kindly upload file of size less than 130MB.');
      }
      this._appUtilsService.loadingImmediateEnded();
    } else {
      this.uploadFilesCount = this.files.length;
      this.ns.error('Multiple sources of data are not allowed.');
      this._appUtilsService.loadingImmediateEnded();
    }
  }

  allowDrop(event) {
    event.preventDefault();
  }

  onDrop(event) {
    event.preventDefault();
    if(event.dataTransfer.files.length == 1){
    for (let i = 0; i < event.dataTransfer.files.length; i++) {
      this.files.push(event.dataTransfer.files[i]);
    }

    this.sourceTotalLength = this.files.length;
  }else{
    this.ns.error('Multiple sources of data are not allowed.');
  }

  }

  public removeFile(fileAtIndex: number, fileName: string) {
    this.files.splice(fileAtIndex, 1);
    const index = this.selectedParentFile.findIndex(x => (x === fileName));
    if (index >= 0) {
      this.selectedParentFile = [];
    }

    this.sourceTotalLength = this.files.length + this.entityArray.length;
    this.fileInput.nativeElement.value = '';
  }

  loaderFinishedinjestIEDataCheck(dataOnceLoaderScreenCompleted) {
    this.loaderScreenIE = false;
    this.sourceTotalLength = 0;
    if (this.env === 'FDS') {
      this.setEntityView();
    } else if (this.env === null) {
      this.setFileView();
    }
    if (dataOnceLoaderScreenCompleted.Status === 'E') {
      //this.checkModelAvailableOrNot();
    } else {
      //this.checkModelAvailableOrNot();
    }
  }

  /* ------------------- Initialize Property actions ------------------------*/
  onSubmit() {
    let entityDeliveryType = null;
    let entityStartDate = null;
    let entityEndDate = null;
    let entityNameVal = null;
    if (this.entityFormGroup) {
      entityDeliveryType = this.entityFormGroup.get('deliveryTypeControl').value;
      entityStartDate = this.entityFormGroup.get('startDateControl').value;
      entityEndDate = this.entityFormGroup.get('endDateControl').value;
      entityNameVal = this.entityFormGroup.get('entityControl').value;
    }
    let isEntityEmpty = false;
    let isDeliveryEmpty = false;
    if (Array.isArray(entityNameVal)) {
      if (entityNameVal.length === 0) {
        isEntityEmpty = true;
      }
    } else {
      if (this.coreUtilService.isNil(entityNameVal)) {
        isEntityEmpty = true;
      }
    }

    if (this.coreUtilService.isNil(entityDeliveryType)) {
      isDeliveryEmpty = true;
    }

    let allEntityfieldEmpty = false;
    if ((this.coreUtilService.isNil(entityStartDate) && this.coreUtilService.isNil(entityEndDate)
      && isEntityEmpty === true && isDeliveryEmpty === true)) {
      allEntityfieldEmpty = true;
    }

    if ((this.coreUtilService.isNil(entityStartDate) || this.coreUtilService.isNil(entityEndDate)
      || isEntityEmpty === true || isDeliveryEmpty === true) && !allEntityfieldEmpty) {
      // Validation - To check if entity is selected and all the values are filled
      this.ns.error('Fill all the fields for Entity');
    } else { // Forming the payload
      
      const entityObject = {};
      const entityPayload = { 'pad': {} };
      const instMLPayload = { 'InstaMl': {} };
      const metricsPayload = { 'metrics': {} };
      const filePayload = { 'fileUpload': {} };
      const entitiesNamePayload = { 'EntitiesName': {} };
      const metricsNamePayload = { 'MetricNames': {} };
      const customPayload = { 'Custom': {} };

      const payload = {
        'source': undefined,
        'sourceTotalLength': this.sourceTotalLength,
        'parentFileName': undefined,
        'uploadFileType': this.sourceTotalLength === 1 ? 'single' : 'multiple',
        // 'category': this.selectedDeliveryType ? this.selectedDeliveryType.toUpperCase() : undefined,
        'category': this.selectedDeliveryType && !(this.env == 'FDS' && (this.requestType == 'AM' || this.requestType == 'IO'))
          ? this.selectedDeliveryType.toUpperCase() : undefined,
        'mappingFlag': false,
        'uploadType': 'FileDataSource',
        'PIConfirmation': false,
        'CorrelationId': '',
        'oldCorrelationId' : undefined,
        'languageFlag': this.language
      };


      const finalPayload = [];
      let entityArrayLength = this.entityArray.length;
      //*****  Payload for Entity ********/
      if (entityArrayLength >= 1) {

        payload.source = 'Entity';
        if (this.selectedParentType) {
          if (this.selectedParentType === 'file') {
            payload.parentFileName = this.selectedParentFile[0];
          } else if (this.selectedParentType === 'Custom') {
            payload.parentFileName = this.selectedParentFile[0];
          } else {
            payload.parentFileName = this.selectedParentFile[0] + '.Entity';
          }
        }

        if (this.selectedParentType === undefined) {
          this.selectedParentType = payload.source;
        }
        //  const entityObject = {};
        if (this.env === 'FDS' && (this.requestType === 'AM' || this.requestType === 'IO')) {
          let selectedEntityObj = { 'name': '', 'sub': '' };
          this.entityList2.forEach(element => {
            if (element.name == this.selectedEntity) {
              selectedEntityObj = element;
            }
          });
          entityObject[selectedEntityObj.name] = selectedEntityObj.sub;
        } else {
          this.entityArray.forEach(entity => {
            const selectedEntity = this.entityList.filter(x => x.name === entity);
            entityObject[entity] = selectedEntity[0].sub;
          });

        }

        if (!(this.env == 'FDS' && (this.requestType == 'AM' || this.requestType == 'IO'))) {
          entitiesNamePayload['EntitiesName'] = this.entityArray.join(',');
          entityPayload.pad['startDate'] = this.entityFormGroup.get('startDateControl').value.toLocaleDateString();
          entityPayload.pad['endDate'] = this.entityFormGroup.get('endDateControl').value.toLocaleDateString();
          entityPayload.pad['Entities'] = entityObject;
          entityPayload.pad['method'] = this.selectedDeliveryType?.toUpperCase();
          payload.source = this.selectedParentType;
        } else {
          entityPayload.pad = {};
          instMLPayload.InstaMl = {};
          metricsPayload.metrics = {};
          metricsNamePayload.MetricNames = {};
          customPayload.Custom['ServiceType'] = this.selectedEntity
          customPayload.Custom['startDate'] = this.entityFormGroup.get('startDateControl').value.toLocaleDateString();
          customPayload.Custom['endDate'] = this.entityFormGroup.get('endDateControl').value.toLocaleDateString();
          customPayload.Custom['Environment'] = this.env;
          customPayload.Custom['RequestType'] = this.requestType;
          payload.source = 'Custom';
          payload.category = this.requestType;
        }


        //  payload.source = this.selectedParentType;
        payload.sourceTotalLength = this.sourceTotalLength;
        finalPayload.push(payload);
        finalPayload.push(entityPayload);
        finalPayload.push(instMLPayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(filePayload);

        if (this.env == 'FDS' && (this.requestType == 'AM' || this.requestType == 'IO')) {
          entitiesNamePayload['EntitiesName'] = {};
          finalPayload.push(entitiesNamePayload);
          finalPayload.push(metricsNamePayload);
          finalPayload.push(customPayload);
        } else {
          finalPayload.push(entitiesNamePayload);
          finalPayload.push(metricsNamePayload);
        }

        this.outPutData(finalPayload, payload.source);
      }
      if (this.files.length > 0) { // Payload for File
        payload.source = 'File';
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
          if (entityArrayLength >= 1) {
            payload.source = 'File';
            payload.category = this.requestType;
          }
        }
        if (this.selectedParentType === undefined) {
          this.selectedParentType = payload.source;
        }
        filePayload.fileUpload['filepath'] = this.files;
        // if (this.fileView === true) {
        //   payload.oldCorrelationId = localStorage.getItem('oldCorrelationID') ? localStorage.getItem('oldCorrelationID') : undefined;
        // }

        filePayload.fileUpload['filepath'] = this.files;
        if (this.sourceTotalLength !== 1) {
          if (this.selectedParentType === 'file') {
            payload.parentFileName = this.selectedParentFile[0];
          } else if (this.selectedParentType === 'Metric') {
            payload.parentFileName = 'Metric.Metric';
          } else if (this.selectedParentType === 'Custom') {
            payload.parentFileName = this.selectedParentFile[0];
          } else {
            payload.parentFileName = this.coreUtilService.isNil(this.selectedParentFile[0]) ? undefined : this.selectedParentFile[0] + '.Entity';
          }
        } else {
          payload.parentFileName = undefined;
          payload.source = this.selectedParentType;
          payload.sourceTotalLength = 1;
        }
        finalPayload.push(payload);
        finalPayload.push(entityPayload);
        finalPayload.push(instMLPayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(filePayload);
        finalPayload.push(entitiesNamePayload);
        finalPayload.push(metricsNamePayload);
        if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
          finalPayload.push(customPayload);
        }
        this.outPutData(finalPayload, payload.source);
        // this.dialog.close(finalPayload);
      }
    }
    

  }

  outPutData(finalPayload, source){
    this.uploadedData.emit({finalPayload : finalPayload, source : source});
  }

}
