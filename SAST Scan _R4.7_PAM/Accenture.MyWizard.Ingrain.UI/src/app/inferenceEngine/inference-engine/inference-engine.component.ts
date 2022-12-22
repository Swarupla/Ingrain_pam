import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap } from 'rxjs/operators';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { ActivatedRoute, Router } from '@angular/router';
import { UploadIEDataService } from './upload-IEData.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { IeConfigurationService } from '../ie-configuration/ie-configuration.service';
import { InferenceModelsService } from '../inference-models/inference-models.service';
import { InferenceModelsComponent } from '../inference-models/inference-models.component';
import { PublishedUseCaseComponent } from '../published-use-case/published-use-case.component';
import * as _ from 'lodash';
import { DatePipe } from '@angular/common';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';


interface IParamData {
  clientUID: any;
  parentUID: any;
  deliveryConstructUId: any;
}

@Component({
  selector: 'app-inference-engine',
  templateUrl: './inference-engine.component.html',
  styleUrls: ['./inference-engine.component.css']
})
export class InferenceEngineComponent implements OnInit {

  @ViewChild('fileInput', { static: false }) fileInput: any;
  @ViewChild('searchInput', { static: false }) searchInput: any;
  @ViewChild(InferenceModelsComponent) InferenceModelsComponent: InferenceModelsComponent;
  @ViewChild(PublishedUseCaseComponent) PublishedUseCaseComponent: PublishedUseCaseComponent

  isnavBarToggle: boolean; // header related
  toggle: boolean = true;
  paramData = {} as IParamData;
  fileName;
  allowedFileType;
  allowedExtensions = ['.xlsx', '.csv'];
  files = [];

  fileView: boolean;
  entityView: boolean;
  entityFormGroup: FormGroup;
  entityList = [];
  allEntities = [];
  entityArray = [];
  deliveryTypeList: Array<string>;
  selectedDeliveryType: string;
  selectedEntity;
  entityLoader = true;
  modelName = '';

  payloadForGetAICoreModels = {
    'clientid': '',
    'dcid': '',
    'serviceid': '',
    'userid': ''
  };

  Myfiles: any;
  selectedParentFile: any = [];
  env: string;
  requestType: string;
  selectedParentType: any;
  sourceTotalLength: number;
  agileFlag: boolean;
  uploadFilesCount: number;
  removalEntityFlag: any;
  entityselection: boolean;

  processingIEData;
  loaderScreenIE: boolean;
  showListOfModels: boolean;
  listOfModels: Array<any>;
  listOfModelsCopy: Array<any>;
  queryParams;
  startDate = new Date();
  enablePublishUseCaseTab;
  applicationList;
  FileEntityName: string;
  isNavBarLabelsToggle = false;
  searchText;
  publishedUseCasesList = [];
  entityList2 = [];

  today = new Date();
  minDate;
  maxDate;

  constructor(private problemStatementService: ProblemStatementService, private ns: NotificationService, private formBuilder: FormBuilder,
    private dialogService: DialogService, private coreUtilService: CoreUtilsService, public router: Router,
    private iEService: UploadIEDataService, private _appUtilsService: AppUtilsService, private activatedRoute: ActivatedRoute,
    private ieConfigService: IeConfigurationService, private inferenceModelService: InferenceModelsService, private datePipe: DatePipe,
    private envService : EnvironmentService,private msalAuthentication: AuthenticationService) {
    if (localStorage.getItem('CustomFlag')) {
      this.agileFlag = true;
    } else {
      this.agileFlag = false;
    }
  }

  ngOnInit(): void {
    this.activatedRoute.queryParams
      .subscribe(params => {
        this.queryParams = params;
        this.ieConfigService.params = params;
        //check URL for redirection from VDS.
        if (!this.coreUtilService.isNil(this.queryParams.UserId)) {
          console.log('External Redirection',this.queryParams.UserId);
          this.verifyMsalToken();
        } else {
          console.log('Internal Redirection');
          this.getLoadPageData();
        }
      });
  }

  /**
   * Verify MSAL token, check for active account before loading IE.
   */
  verifyMsalToken() {
    if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
      if (!this.msalAuthentication.msalService.instance.getAllAccounts().length) {
        console.log('NO Active account found, Extrenal IE, redirecting to MSAL Authentication.');
        this.msalAuthentication.login();
      } else {
        this.msalAuthentication.getToken().subscribe(data => {
          if (data) {
            console.log('Loading IE after getting MSAL token.');
            this.envService.setMsalToken(data);
            this.getLoadPageData();
          }
        });
      }
    }
  }

  /**
   * get the data initials required to load IE.
   */
  getLoadPageData(): void {
    this.env = sessionStorage.getItem('Environment');
    // for date picker date should be only less than 2 years from the current year
    if (this.env === 'FDS' || this.env === 'PAM') {
      this.minDate = new Date(this.today.getFullYear() - 2, (this.today.getMonth()), (this.today.getDate()));
      this.maxDate = new Date(this.today.getFullYear(), 11, 31);
    }
    this.requestType = sessionStorage.getItem('RequestType');
    this.removalEntityFlag = false;
    this.entityselection = false;
    this.loaderScreenIE = false;
    this.enablePublishUseCaseTab = false;
    this.sourceTotalLength = 0;
    if (this.env === 'FDS' || this.env === 'PAM') {
      this.setEntityView();
    } else if (this.env === null) {
      this.setFileView();
    }
    this.setFileView();
    this._appUtilsService.getParamData().subscribe(data => {
      if (data) {
        this.paramData = data
        this.checkModelAvailableOrNot();
      }
    });
    this.getApplicationName();
  }

  /* ------------------- Get List of All IE Model ------------------------*/
  checkModelAvailableOrNot() {
    this.showListOfModels = false;
    this.listOfModels = [];
    this.listOfModelsCopy = [];
    this.applicationList = [];
    if (this.env === 'FDS' || this.env === 'PAM') {
      this.setEntityView();
    } else if (this.env === null) {
      this.setFileView();
    }
    this.iEService.getIEModel(this.paramData.clientUID, this.paramData.deliveryConstructUId, this._appUtilsService.getCookies().UserId).subscribe(
      (data) => {
        if (data.length > 0) {
          this.showListOfModels = true;
          this.listOfModels = data;
          this.listOfModelsCopy = data;
        } else {
          this.showListOfModels = false;
          this.listOfModels = [];
          this.listOfModelsCopy = [];
        }
      },
      (error) => {
        this.ns.error(error.error);
      }
    )
  }

  getApplicationName() {
    const env = sessionStorage.getItem('Environment') ? sessionStorage.getItem('Environment') : '';

    this.inferenceModelService.getApplications(env).subscribe(
      (data) => {
        if (data.length > 0) {
          this.applicationList = data;
          //filtering for VDS(AIOPS) applicationIds
          data.forEach((app) => {
            if (env === 'FDS') {
              if (app.ApplicationID == "9102fb74-5deb-46ff-9798-9fe6b20945f3") {
                this.ieConfigService.application.ApplicationID = app.ApplicationID;
                this.ieConfigService.application.ApplicationName = app.ApplicationName;
              }
            } else if (env === 'PAM') {
              if (app.ApplicationName == "VDS") {
                this.ieConfigService.application.ApplicationID = app.ApplicationID;
                this.ieConfigService.application.ApplicationName = app.ApplicationName;
              }
            } else {
              if (app.ApplicationID == "65063df1-7a20-4fb2-9da5-b5800f2ca48c") {// for SI flow.
                this.ieConfigService.application.ApplicationID = app.ApplicationID;
                this.ieConfigService.application.ApplicationName = app.ApplicationName;
              }
            }

          });
        }
      }, (error) => {
        this.applicationList = [];
      })
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
      // const entityPayload = { 'pad': {} };
      // const filePayload = { 'fileUpload': {} };
      // const entitiesNamePayload = { 'EntitiesName': {} };
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
        'category': this.selectedDeliveryType && !((this.env == 'FDS' || this.env === 'PAM') && (this.requestType == 'AM' || this.requestType == 'IO'))
          ? this.selectedDeliveryType.toUpperCase() : undefined,
        'mappingFlag': false,
        'uploadType': 'FileDataSource',
        'DBEncryption': false,
        'InferenceEngine': true,
        'CorrelationId': '',
        'entityStartDate' : entityStartDate,
        'entityEndDate' : entityEndDate
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
        if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
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

          // let selectedEntityObj = { 'name': '', 'sub': '' };
          // this.entityList.forEach(element => {
          //   if (element.name == this.selectedEntity) {
          //     selectedEntityObj = element;
          //   }
          // });
        }

        if (!((this.env == 'FDS' || this.env === 'PAM') && (this.requestType == 'AM' || this.requestType == 'IO'))) {
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

        if ((this.env == 'FDS' || this.env === 'PAM') && (this.requestType == 'AM' || this.requestType == 'IO')) {
          entitiesNamePayload['EntitiesName'] = {};
          finalPayload.push(entitiesNamePayload);
          finalPayload.push(customPayload);
        } else {
          finalPayload.push(entitiesNamePayload);
        }

        finalPayload.push(instMLPayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(filePayload);
        finalPayload.push(metricsNamePayload);

        this.Myfiles = finalPayload;
        this.showModelEnterDialog(finalPayload, payload.source);
      }
      if (this.files.length > 0) { // Payload for File
        payload.source = 'File';
        if (this.selectedParentType) {
          if (this.selectedParentType === 'file') {
            payload.parentFileName = this.selectedParentFile[0];
          } else if (this.selectedParentType === 'Custom') {
            payload.parentFileName = this.selectedParentFile[0];
          } else {
            payload.parentFileName = this.selectedParentFile[0] + '.Entity';
          }
        }

        filePayload.fileUpload['filepath'] = this.files;
        finalPayload.push(payload);
        finalPayload.push(filePayload);
        finalPayload.push(instMLPayload);
        finalPayload.push(entityPayload);
        finalPayload.push(entitiesNamePayload);
        finalPayload.push(metricsPayload);
        finalPayload.push(metricsNamePayload);
        this.Myfiles = finalPayload;
        this.showModelEnterDialog(finalPayload, payload.source);
        // this.dialog.close(finalPayload);
      }
    }

  }

  /*-------------------- Enter Model Name Dialog ----------------------------*/
  showModelEnterDialog(filesData, source) {
    const openTemplateAfterClosed =
      this.dialogService.open(TemplateNameModalComponent, { data: { title: 'Enter Model Name', pageInfo: 'InferenceEngine', source: source } }).afterClosed.pipe(
        tap(data => data ? this.showLoaderScreenIE(filesData, data.ModelName, data.EntityName) : '')
      );

    openTemplateAfterClosed.subscribe();
  }

  /*--------------------- Show File Upload Process Dialog -------------------------*/
  showLoaderScreenIE(filesData, modelName: string, entityName?: string) {
    this.modelName = modelName;
    this.FileEntityName = entityName;
    this.processingIEData = filesData;
    this.files = [];
    if (this.fileInput) { this.fileInput.nativeElement.value = '' };

    this._appUtilsService.loadingStarted();
    this.iEService.checkIEModel({
      'ModelName': modelName,
      'clientUID': this.paramData.clientUID,
      'deliveryUID': this.paramData.deliveryConstructUId,
      'userId': this._appUtilsService.getCookies().UserId
    }).subscribe(
      (data) => {
        if (data) {
          this.ns.error('Model name already exists')
          this.loaderScreenIE = false;
        } else {
          this.loaderScreenIE = true;
        }
        if (this.fileInput) { this.fileInput.nativeElement.value = '' };
        this._appUtilsService.loadingEnded();
      },
      (error) => {
        if (this.fileInput) { this.fileInput.nativeElement.value = '' };
        this.ns.error(error.error);
        this._appUtilsService.loadingEnded();
      }
    )
    // this.loaderScreenIE = true;
  }


  /* ------------------- Upload File Related ------------------------*/
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
    if (this.sourceTotalLength < 5 && resourceCount <= (5 - this.sourceTotalLength)) {
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
            if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
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
      this.ns.error('Maximum 5 sources of data allowed.');
      this._appUtilsService.loadingImmediateEnded();
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

    this.sourceTotalLength = this.files.length;

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


  /* ------------------- Upload Entity Related ------------------------*/
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

    if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      this.deliveryTypeList = ['AIops'];
    } else {
      this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others', 'PPM'];
    }

    if ((this.env === 'FDS' || this.env === 'PAM') && this.requestType === 'AM') {
      this.entityLoader = false;
      this.entityList2 = [{ 'name': 'Incidents', 'sub': 'ALL' },
      { 'name': 'ServiceRequests', 'sub': 'ALL' },
      { 'name': 'ProblemTickets', 'sub': 'ALL' },
      { 'name': 'WorkRequests', 'sub': 'ALL' },
      { 'name': 'CHANGE_REQUEST', 'sub': 'ALL' }]; // 
    } else if ((this.env === 'FDS' || this.env === 'PAM') && this.requestType === 'IO') {
      this.entityLoader = false;
      this.entityList2 = [{ 'name': 'Incidents', 'sub': 'ALL' },
      { 'name': 'ServiceRequests', 'sub': 'ALL' },
      { 'name': 'ProblemTickets', 'sub': 'ALL' },
      { 'name': 'ChangeRequests', 'sub': 'ALL' }];
    }

    if (this.entityLoader) {
      this.entityList = [];
      // this.deliveryTypeList = ['Agile', 'Devops', 'AD', 'Others'];
      this.getEntityDetails();
    }
  }

  getEntityDetails() {
    this.entityList = [];
    this.allEntities = [];
    this.entityArray = [];
    this.problemStatementService.getDynamicEntity(this.paramData.clientUID, this.paramData.deliveryConstructUId, this._appUtilsService.getCookies().UserId).subscribe(
      data => {
        // this..loadingEnded();
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

        // Filtering entity data on the basis of deliveryTypeName
        /* this.entityList = this.entityList.filter(x => {
          const deliveryTypes = x.subDeliverable.split('/');
          if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
            return x;
          }
        }); */

        if (this.entityList.length === 0) {
          this.entityFormGroup.get('startDateControl').disable();
          this.entityFormGroup.get('endDateControl').disable();
        }
      },
      error => {
        // this.apputilService.loadingEnded();
        this.entityLoader = false;
        this.ns.error('Pheonix metrics api has no data.');
      });
  }


  removeEntity(index: number, entityName: string) {
    if (this.env !== 'FDS' && this.env !== 'PAM' && this.requestType !== 'AM' && this.requestType !== 'IO') {
      this.entityArray = this.entityArray.filter(e => e !== entityName);
    }
    let entityArrayLength = this.entityArray.length;
    if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      entityArrayLength = 0;
      this.removalEntityFlag = true;
      this.entityArray = [];
      this.entityFormGroup.get('entityControl').setValue(this.entityArray);
    }
    this.sourceTotalLength = this.files.length + entityArrayLength;
    this.entityFormGroup.get('entityControl').setValue(this.entityArray);
  }

  /* ------------------- Upload File and Enity Related ------------------------*/
  public setParentFile(selectedParentName, type) {
    this.selectedParentFile[0] = selectedParentName;
    this.selectedParentType = type;
  }

  /* -------- header related function ----- */
  previous() {
    this.router.navigate(['choosefocusarea']);
  }

  toggleNavBar() {
    this.isnavBarToggle = !this.isnavBarToggle;
  }
  /* -------- header relaed function ----- */

  loaderFinishedinjestIEDataCheck(dataOnceLoaderScreenCompleted) {
    this.loaderScreenIE = false;
    this.sourceTotalLength = 0;
    if (this.env === 'FDS' || this.env === 'PAM') {
      this.setEntityView();
    } else if (this.env === null) {
      this.setFileView();
    }
    if (dataOnceLoaderScreenCompleted.Status === 'E') {
      this.checkModelAvailableOrNot();
    } else {
      this.checkModelAvailableOrNot();
    }
  }

  onChangeOfDeliveryType(deliveryName) {
    const deliveryTypeValue = this.entityFormGroup.get('deliveryTypeControl').value;
    //  this.entityList = [];
    // this.deliveryTypeSelection = true; Not in use
    let deliveryTypeValueLength = deliveryTypeValue.length;
    deliveryTypeValueLength = 1;
    // const sourceLength = this.uploadedData.length + deliveryTypeValueLength;
    // if (sourceLength < 2) {

    this.entityList = this.allEntities.filter(x => {
      const deliveryTypes = x.sub.split('/');
      // if (deliveryTypes.indexOf(deliveryName) !== -1 || deliveryTypes.indexOf('ALL') !== -1) {
      //   return x;
      // }
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
    // this.sourceTotalLength = this.uploadedData.length + deliveryTypeValueLength;
    // }
    // this.disableDownLoadTeamplate = false
  }

  onChangeOFEntity() {
    const entityValue = this.entityFormGroup.get('entityControl').value;
    this.entityselection = true;
    let entityValueLength = entityValue.length;
    let entityArrayLength = 0;
    if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      entityValueLength = 1;
    }
    const sourceLength = this.files.length + entityValueLength;
    if (sourceLength <= 5) {
      if (this.entityFormGroup.get('startDateControl').status === 'VALID' &&
        this.entityFormGroup.get('endDateControl').status === 'VALID') {
        this.entityArray = this.entityFormGroup.get('entityControl').value;
        if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
          let selectedEntity = [];
          selectedEntity.push(this.entityFormGroup.get('entityControl').value);
          this.entityArray = selectedEntity;
          entityArrayLength = this.entityArray.length;
        } else {
          entityArrayLength = this.entityArray.length;
        }

        if ((this.env === 'FDS' || this.env === 'PAM') && (this.requestType === 'AM' || this.requestType === 'IO')) {
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
      //  if (this.env !== 'FDS' && (this.requestType !== 'AM' || this.requestType !== 'IO')) {
      this.entityFormGroup.get('entityControl').setValue(this.entityArray);
      // }
    }
  }

  loadModel() {
    this.enablePublishUseCaseTab = false;
    this.checkModelAvailableOrNot();
  }

  loadUseCases() {
    this.enablePublishUseCaseTab = true;
  }

  checkColor() {
    if (!this.enablePublishUseCaseTab) {
      const styles = {
        'background-color': '#000088',
        'color': 'white'
      };
      return styles;
    } else {
      const styles = {
        'background-color': 'white',
        'color': 'black'
      };
      return styles;
    }
  }

  checkColorPublish() {
    if (this.enablePublishUseCaseTab) {
      const styles = {
        'background-color': '#000088',
        'color': 'white'
      };
      return styles;
    } else {
      const styles = {
        'background-color': 'white',
        'color': 'black'
      };
      return styles;
    }
  }


  searchIEModel(value) {
    const data = this.listOfModelsCopy.filter(
      (model) => {
        let string = JSON.stringify(model.ModelName + "" + model.Entity)
        if (model.Status === 'C') { string = string + 'Completed'; }
        if (model.Status === 'P') { string = string + 'In-Progress' }
        if (model.Status === 'E') { string = string + 'Error' }
        return (string.toLocaleLowerCase().includes(value));
      })
    if (this.InferenceModelsComponent) {
      this.InferenceModelsComponent.uploadedInferencesList = data;
    }

    if (this.enablePublishUseCaseTab) {
      let publishData // = this.publishedUseCasesList;

      publishData = _.filter(this.publishedUseCasesList, function (item) {
        return item.UseCaseName.indexOf(value) > -1;
      });

      if (this.PublishedUseCaseComponent) {
        this.PublishedUseCaseComponent.publishedUseCases = publishData;
      }
    }
  }

  closeSearchBox() {
    // if (this.searchInput.length > 0) { this.searchInput.nativeElement.value = '' };
    if (this.InferenceModelsComponent) {
      this.InferenceModelsComponent.uploadedInferencesList = this.listOfModelsCopy;
    }

    if (this.enablePublishUseCaseTab) {
      if (this.PublishedUseCaseComponent) {
        this.PublishedUseCaseComponent.publishedUseCases = this.publishedUseCasesList;
      }
    }

    this.searchInput = '';
  }

  toggleNavBarLabels() {
    this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
    return false;
  }

  publishUsecaseData(publishUsecaseList) {
    this.publishedUseCasesList = publishUsecaseList;
  }
}
