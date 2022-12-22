import { Component, OnInit, ElementRef, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { Validators, FormBuilder, FormGroup } from '@angular/forms';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { ProblemStatementService } from '../../_services/problem-statement.service';
import * as $ from 'jquery';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { serviceLevel } from 'src/app/_enums/service-types.enum';
import { DialogService } from 'src/app/dialog/dialog.service';
import { UserNotificationpopupComponent } from '../user-notificationpopup/user-notificationpopup.component';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
declare var userNotification: any;

@Component({
  selector: 'app-upload-similarity-analysis-api',
  templateUrl: './upload-similarity-analysis-api.component.html',
  styleUrls: ['./upload-similarity-analysis-api.component.scss']
})
export class UploadSimilarityAnalysisAPIComponent implements OnInit, AfterViewInit {

  correlationId: any;
  Myfiles: any;
  modelName: any;
  entityList = [];
  selectedEntity;
  startDate = new Date();
  apiRow: FormGroup;
  files = [];
  uploadStatus: string;
  uploadFilesCount = 0;
  selectedParentFile = [];
  entityArray = [];
  sourceTotalLength: number;
  allowedExtensions = ['.xlsx', '.csv'];
  selectedParentType;
  env;
  requestType;
  entityVal;
  PIConfirmation;
  prebuiltFlag: boolean;
  removalEntityFlag: boolean;
  EntityData: string;
  entityselection: boolean;
  deliveryTypeSelection: boolean;
  deliveryTypeList = [];
  selectedDeliveryType: string;
  reTrainModel: boolean;
  serviceId;
  disableDownLoadTeamplate = true;
  screenSelectedForUpload = '';
  entityLoader = false;
  allEntities = [];
  language: string;

  fileView: boolean;
  entityView: boolean;
  datasetView: boolean;
  customDataView: boolean;
  isAdminUser : boolean = false;
  isQueryViewEnabled: boolean = true;
  isApiViewEnabled: boolean;
  retrainFlag = false;
  ApiConfig = {
    isDataSetNameRequired : false,
    isCategoryRequired : false,
    isIncrementalRequired : false
  };

  selectedsource;
  choosesource = [
    { name: "File", value: "File" },
    { name: "API", value: "API" },
  ];
  datasetname;
  datasetname2;
  choosetype = [];
  datasetArray = [];
  datasetEntered: boolean = false;
  selectedtype;
  selectedDataSetSourcetype = '';
  serviceLevel = serviceLevel.AI;
  instanceType : string;

  constructor(private ns: NotificationService, private formBuilder: FormBuilder,
    private coreUtilsService: CoreUtilsService,
    public dialog: DialogRef, private dialogConfig: DialogConfig, private problemStatementService: ProblemStatementService,
    private _appUtilsService: AppUtilsService, private dialogService: DialogService, private environmentService : EnvironmentService) {
  }

  ngOnInit() {
    this.instanceType = sessionStorage.getItem('Instance');
    if (this.environmentService.IsPADEnvironment()) {//enable custom data option only for PAD environmant
      this.verifyAdminUser();
    }
    this.env = sessionStorage.getItem('Environment');
    this.requestType = sessionStorage.getItem('RequestType');
    this.removalEntityFlag = false;
    this.entityselection = false;
    this.deliveryTypeSelection = false;
    if (sessionStorage.getItem('Language')) {
      this.language = sessionStorage.getItem('Language').toLowerCase();
    } else {
      this.language = 'english';
    }

    if (this.dialogConfig.hasOwnProperty('data')) {
      this.serviceId = this.dialogConfig.data.serviceId;
      this.screenSelectedForUpload = this.dialogConfig.data.actionScreen;
    }

    if (this.dialogConfig.hasOwnProperty('data') && this.dialogConfig.data.entityData === undefined) {
      // this.deliveryTypeList = Object.keys(this.dialogConfig.data.entityData);
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        this.deliveryTypeList = ['AIops'];
      } else {
        this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others', 'PPM'];
      }
    }

    if (this.dialogConfig.hasOwnProperty('data') && this.dialogConfig.data.retrainModel !== undefined) {
      this.reTrainModel = this.dialogConfig.data.retrainModel;
    }

    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });

    if (!this.coreUtilsService.isNil(this.dialogConfig.data) && !this.coreUtilsService.isNil(this.dialogConfig.data['Flag'])) {
      this.prebuiltFlag = true;
    }

    if (this.reTrainModel) {
      const datasource = this.dialogConfig.data.selectedModelData["Source"].toLowerCase();
      if (datasource === 'DataSet'.toLowerCase() || datasource === 'File_DataSet'.toLowerCase() || datasource === 'ExternalAPIDataSet'.toLowerCase()) {
        this.setDatasetView();
      } else if (datasource === "Entity".toLowerCase() || datasource === "Phoenix".toLowerCase()) {
          this.setEntityView();
          this.apiRow.get('startDateControl').enable();
          this.apiRow.get('endDateControl').enable();
          this.apiRow.get('entityControl').disable();
          this.apiRow.get('deliveryTypeControl').disable();
          // this.disableDownLoadTeamplate = false;

          if (this.env == 'PAM' || this.env == 'FDS') {            
           // const entityObj = this.dialogConfig.data.selectedModelData['custom'];
            const entity = this.dialogConfig.data.selectedModelData['custom'].InputParameters.ServiceType;
            this.apiRow.controls.deliveryTypeControl.setValue(this.deliveryTypeList[0]); 
            this.apiRow.controls.entityControl.setValue(entity);
            this.entityList.push({ 'name': entity, 'sub': this.deliveryTypeList[0] });
          this.selectedEntity = entity;
          this.selectedDeliveryType = this.deliveryTypeList[0];
          this.entityArray[0] = { 'name': entity, 'sub': this.deliveryTypeList[0] };
          } else {
          const entityObj = JSON.parse(this.dialogConfig.data.selectedModelData['pad']);
          const entity = Object.keys(entityObj.Entities)[0];

          this.apiRow.controls.deliveryTypeControl.setValue(entityObj.method);
          this.apiRow.controls.entityControl.setValue(entity);
          this.entityList.push({ 'name': entity, 'sub': entityObj.Entities[entity] });
          this.selectedEntity = entity;
          this.selectedDeliveryType = entityObj.method;
          this.entityArray[0] = { 'name': entity, 'sub': entityObj.Entities[entity] };
          }
          this.sourceTotalLength = this.files.length + this.entityList.length;
        } else if (datasource === "File".toLowerCase()) {
          this.setFileView();
          this.apiRow.get('startDateControl').disable();
          this.apiRow.get('endDateControl').disable();
          this.apiRow.get('entityControl').disable();
          this.apiRow.get('deliveryTypeControl').disable();
          //  this.disableDownLoadTeamplate = true;
        } else if (datasource === 'Custom'.toLowerCase() || datasource === 'CustomDataAPI'.toLowerCase()) {
          this.setCustomDataView();
        }
      }
  }

  ngAfterViewInit() { }

  onSubmit() {
    if (this.dialogConfig.data.dataSetUId === undefined || this.datasetname === undefined) {
      if (this.sourceTotalLength >= 1) { // Validation - To check if atleast one source is uploaded
        const entityDeliveryType = this.apiRow.get('deliveryTypeControl').value;
        const entityStartDate = this.apiRow.get('startDateControl').value;
        const entityEndDate = this.apiRow.get('endDateControl').value;
        const entityNameVal = this.apiRow.get('entityControl').value;
        let isEntityEmpty = false;
        let isDeliveryEmpty = false;
        if (Array.isArray(entityNameVal)) {
          if (entityNameVal.length === 0) {
            isEntityEmpty = true;
          }
        } else {
          if (this.coreUtilsService.isNil(entityNameVal)) {
            isEntityEmpty = true;
          }
        }

        if (this.coreUtilsService.isNil(entityDeliveryType)) {
          isDeliveryEmpty = true;
        }

        let allEntityfieldEmpty = false;
        if ((this.coreUtilsService.isNil(entityStartDate) && this.coreUtilsService.isNil(entityEndDate)
          && isEntityEmpty === true && isDeliveryEmpty === true)) {
          allEntityfieldEmpty = true;
        }

        if ((this.coreUtilsService.isNil(entityStartDate) || this.coreUtilsService.isNil(entityEndDate)
          || isEntityEmpty === true || isDeliveryEmpty === true) && !allEntityfieldEmpty) {
          // Validation - To check if entity is selected and all the values are filled
          this.ns.error('Fill all the fields for Entity');
        } else { // Forming the payload
          const entityPayload = { 'pad': {} };
          const filePayload = { 'fileUpload': {} };
          const entitiesNamePayload = { 'EntitiesName': {} };
          const entityObject = {};

          const payload = {
            'source': undefined,
            'sourceTotalLength': this.sourceTotalLength,
            'parentFileName': undefined,
            'uploadFileType': 'single',
            'category': this.selectedDeliveryType ? this.selectedDeliveryType.toUpperCase() : undefined,
            'mappingFlag': false,
            'uploadType': 'FileDataSource',
            'DBEncryption': undefined,
            'ServiceId': '',
            'similarity': true,
            'CorrelationId': '',
            'languageFlag': this.language
          };
          if (this.dialogConfig.data.selectedModelData != undefined && this.reTrainModel == true)
            payload['CorrelationId'] = this.dialogConfig.data.selectedModelData["CorrelationId"];

          const finalPayload = [];
          let entityArrayLength = this.entityArray.length;
          if (entityArrayLength >= 1) { // Payload for Entity
            payload.source = 'Entity';
            let selectedEntityObj = { 'name': '', 'sub': '' };
            this.entityList.forEach(element => {
              if (element.name == this.selectedEntity) {
                selectedEntityObj = element;
              }
            });
            entityObject[selectedEntityObj.name] = selectedEntityObj.sub;
            entityPayload.pad['startDate'] = this.apiRow.get('startDateControl').value.toLocaleDateString();
            entityPayload.pad['endDate'] = this.apiRow.get('endDateControl').value.toLocaleDateString();
            entityPayload.pad['Entities'] = entityObject;
            entityPayload.pad['method'] = this.selectedDeliveryType.toUpperCase();
            entitiesNamePayload['EntitiesName'] = selectedEntityObj.name;
            finalPayload.push(payload);
            finalPayload.push(entityPayload);
            finalPayload.push(entitiesNamePayload);
            this.dialog.close(finalPayload);
          }
          if (this.files.length > 0) { // Payload for File
            payload.source = 'File';
            if (this.selectedParentType === undefined) {
              this.selectedParentType = payload.source;
            }
            filePayload.fileUpload['filepath'] = this.files;
            finalPayload.push(payload);
            finalPayload.push(filePayload);
            finalPayload.push(entityPayload);
            finalPayload.push(entitiesNamePayload);
            this.dialog.close(finalPayload);
          }
        }
      } else {
        this.ns.error('Kindly upload atleast one source of data.');
      }
    } else {
      let correlationId;
      // if (this.selectedModelData != undefined && this.reTrainModel == true) {
      //   correlationId = this.selectedModelData['CorrelationId'];
      // }

      let uploadType = '';
      if (this.selectedDataSetSourcetype === 'ExternalAPI') {
        uploadType = 'ExternalAPIDataSet';
      } else if (this.selectedDataSetSourcetype === 'File') {
        uploadType = 'File_DataSet';
      }
      const entityPayload = { pad: {} };
      const entitiesNamePayload = { EntitiesName: {} };
      // const DataSetUId = { DataSetUId: this.dialogConfig.data.dataSetUId};
      const DataSetUId = { DataSetUId: this.datasetname };
      const payload = {
        source: 'DataSet',
        sourceTotalLength: 1,
        parentFileName: undefined,
        uploadFileType: 'single',
        category: this.selectedDeliveryType
          ? this.selectedDeliveryType.toUpperCase()
          : undefined,
        mappingFlag: false,
        uploadType: uploadType,
        DBEncryption: undefined,
        ServiceId: '',
        similarity: true,
        CorrelationId: correlationId,
        languageFlag: this.language,
      };
      const finalPayload = [];
      finalPayload.push(payload);
      finalPayload.push(entityPayload);
      finalPayload.push(entitiesNamePayload);
      finalPayload.push(DataSetUId);
      //this.dialog.close(finalPayload);
      /// this.Myfiles = finalPayload;

      this.dialog.close(finalPayload);
    }
  }

  onClose() {
    this.dialog.close();
  }

  downlaodTemplate(value) {
    let self = this;
    if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
      this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
        if (confirmationflag === true) {
          this.callDownloadTemplate(value);
        }
      });
    } else {
      userNotification.showUserNotificationModalPopup();
      $(".notification-button-close").click(function () {
        self.callDownloadTemplate(value);
      });
    }
  }

  private callDownloadTemplate(value){
    if (value === 'next_word_prediction') {
      window.location.href = "assets/files/next_word_prediction.xlsx";
    }
    else if (value === 'insurance') {
      window.location.href = "assets/files/insurance.csv";
    } else if (value === 'similarity') {
      window.location.href = "assets/files/similarity_analysis.xlsx";
    }
  }

  getFileDetails(e) {
    this.uploadFilesCount += e.target.files.length;
    if (this.uploadFilesCount > 1) {
      this.ns.error('Multiple file upload is not applicable');
      return 0;
    } else {
      let validFileExtensionFlag = true;
      let validFileNameFlag = true;
      let validFileSize = true;
      let fileLimit = 136356582;
      const resourceCount = e.target.files.length;
      // if (this.sourceTotalLength < 5 && resourceCount <= (5 - this.sourceTotalLength)) {
      const files = e.target.files;
      let fileSize = 0;
      for (let i = 0; i < e.target.files.length; i++) {
        const fileName = files[i].name;
        const dots = fileName.split('.');
        const fileType = '.' + dots[dots.length - 1];
        if (!fileName) {
          validFileNameFlag = false;
          break;
        }
        if (this.allowedExtensions.indexOf(fileType) !== -1) {
          fileSize = fileSize + e.target.files[i].size;
          const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
          validFileNameFlag = true;
          validFileExtensionFlag = true;
          if (index < 0) {
            this.files.push(e.target.files[i]);
            this.uploadFilesCount = this.files.length;
            let entityArrayLength = this.entityArray.length;
            this.sourceTotalLength = this.files.length + this.entityArray.length;
            this.apiRow.get('startDateControl').disable();
            this.apiRow.get('endDateControl').disable();
            this.apiRow.get('entityControl').disable();
            this.apiRow.get('deliveryTypeControl').disable();
          }
        } else {
          validFileExtensionFlag = false;
          break;
        }
      }
      if (this.serviceId === '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') { // For Clustering
        fileLimit = 20971520;
      }
      if (fileSize <= fileLimit) {
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
        if (this.serviceId !== '72c38b39-c9fe-4fa6-97f5-6c3adc6ba355') {
          this.ns.error('Kindly upload file of size less than 130MB.');
        } else {
          this.ns.error('Kindly upload file of size less than or equal to 20MB.'); // For Clustering
        }
      }
      // } else {
      //   this.uploadFilesCount = this.files.length;
      //   this.ns.error('Maximum 5 sources of data allowed.');
      // }
    }
  }

  allowDrop(event) {
    event.preventDefault();
  }

  onDrop(event) {
    event.preventDefault();

    for (let i = 0; i < event.dataTransfer.files.length; i++) {
      this.files.push(event.dataTransfer.files[i]);
    }

    this.dialog.close(this.files);
  }

  setFileView() {
    this.fileView = true;
    this.entityView = false;
    this.datasetView = false;
    this.customDataView = false;
    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });
    this.entityList = [];
  }

  setEntityView() {
    this.fileView = false;
    this.entityView = true;
    this.datasetView = false;
    this.customDataView = false;
    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });
  }

  setDatasetView() {
    this.datasetView = true;
    this.fileView = false;
    this.entityView = false;
    this.customDataView = false;
    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });
    this.entityList = [];
  }

  setCustomDataView(){
    this.datasetView = false;
    this.fileView = false;
    this.entityView = false;
    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: [''],
      entityControl: [''],
      deliveryTypeControl: ['']
    });
    this.entityList = [];
    this.customDataView = true;
  }

  onChangeOfDataset() {
    this.datasetEntered = true;
  }

  selectedchoosetype(value) {
    this.datasetname = value;
    const index = this.choosetype.findIndex(x => x.id === value);
    this.datasetname2 = this.choosetype[index].name;
  }

  selectedchoosesource(source) {
    this.choosetype = [];
    // let sourcetype;
    if (source === 'API') {
      this.selectedDataSetSourcetype = 'ExternalAPI';
    } else {
      this.selectedDataSetSourcetype = source;
    }
    this.getdatasetdetails(this.selectedDataSetSourcetype);
  }

  getdatasetdetails(sourcetype) {
    let i = 0;
    this.problemStatementService
      .showdatasetdropdown(
        this.dialogConfig.data.clientUID,
        this.dialogConfig.data.deliveryConstructUID,
        this.dialogConfig.data.userId
      )
      .subscribe((data) => {
        // let dataSetDetails = data.filter(result => result.DataSetUId === this.dialogConfig.data.dataSetUId)       
        // this.selectedDataSetSource = dataSetDetails[0].SourceName;
        // this.selectedDataSetName = dataSetDetails[0].DataSetName;  
        data.forEach((element) => {
          if (element['SourceName'] === sourcetype) {
            const datasets = {
              name: element['DataSetName'],
              id: element['DataSetUId'],
            };
            this.choosetype.push(datasets);
            this.datasetArray[i++] = datasets.name;
          }
        });
      });
  }

  removeDataset() {
    this.datasetEntered = false;
    this.selectedtype = "";
    return false;
  }

  public removeFile(fileAtIndex: number, fileName: string, fileInput) {
    this.files.splice(fileAtIndex, 1);
    const index = this.selectedParentFile.findIndex(x => (x === fileName));
    if (index >= 0) {
      this.selectedParentFile = [];
    }
    this.uploadFilesCount = this.files.length;

    this.sourceTotalLength = this.files.length + this.entityArray.length;
    fileInput = '';
    // this.apiRow.get('deliveryTypeControl').enable();
    return false;
  }

  verifyAdminUser() {
    this._appUtilsService.getRoleData().subscribe(userDetails => {
      const logedinUserRole = userDetails.accessRoleName;
      if (logedinUserRole === 'System Admin' || logedinUserRole === 'Client Admin' ||
        logedinUserRole === 'System Data Admin' || logedinUserRole === 'Solution Architect') {
        this.isAdminUser = true;
      } else {
        this.isAdminUser = false;
      }
    });
  }

  enableQueryView() {
    this.isQueryViewEnabled = true;
    this.isApiViewEnabled = false;
  }

  enableApiView() {
    this.isQueryViewEnabled = false;
    this.isApiViewEnabled = true;
  }

  saveAPI(apiData) {
  let AzureCredentials;
    
    if (apiData[0].clientid !== undefined && apiData[0].clientsecret !== undefined && apiData[0].resource !== undefined && apiData[0].grantype !== undefined) {
      AzureCredentials = {
        'client_id': apiData[0].clientid,
        'client_secret': apiData[0].clientsecret,
        'resource': apiData[0].resource,
        'grant_type': apiData[0].grantype
      };
    } else {
      AzureCredentials = {
        'client_id': '',
        'client_secret': '',
        'resource': '',
        'grant_type': ''
      };
    }
    var apiParams = {
      "startDate": apiData[0].startdate,
      "endDate": apiData[0].enddate,
      "SourceType": CustomDataTypes.API,
      "Data": {
        "METHODTYPE": apiData[0].HttpMethod,
        "ApiUrl": apiData[0].Url,
        "KeyValues": apiData[0].Keyvalue,
        "BodyParam": apiData[0].Body,
        "fetchType": apiData[0].fetchType,
        "Authentication": {
          "Type": apiData[0].authttype,
          "Token": apiData[0].token,
          "AzureUrl": apiData[0].tokenurl,
          'AzureCredentials': AzureCredentials,
          'UseIngrainAzureCredentials' : apiData[0].UseIngrainAzureCredentials
        },
        "TargetNode": apiData[1].targetNode
      }
    };
    let payload={
      source: CustomDataTypes.API,
      CustomDataPull : apiParams
    }
    const entityPayload = { 'pad': {} };
    const filePayload = { 'fileUpload': {} };
    const entitiesNamePayload = { 'EntitiesName': {} };
    const finalPayload = [];
    finalPayload.push(payload);
    finalPayload.push(filePayload);
    finalPayload.push(entityPayload);
    finalPayload.push(entitiesNamePayload);
    this.dialog.close(finalPayload);
  }

  // Save Query custom methods region starts.
  saveQuery(data) {
    let payload={
      source: CustomDataTypes.Query,
      CustomDataPull : data.query
    }
    const entityPayload = { 'pad': {} };
    const filePayload = { 'fileUpload': {} };
    const entitiesNamePayload = { 'EntitiesName': {} };

    const finalPayload = [];
    finalPayload.push(payload);
    finalPayload.push(filePayload);
    finalPayload.push(entityPayload);
    finalPayload.push(entitiesNamePayload);

    this.dialog.close(finalPayload);
  }
  // Save Query custom methods region ends.

}
