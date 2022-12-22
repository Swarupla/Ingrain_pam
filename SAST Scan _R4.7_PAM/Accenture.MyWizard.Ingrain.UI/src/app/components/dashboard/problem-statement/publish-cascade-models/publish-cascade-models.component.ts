import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CascadeModelsService } from 'src/app/_services/cascade-models.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { tap, switchMap, count } from 'rxjs/operators';
import { DeployModelService } from 'src/app/_services/deploy-model.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { empty, Subscription } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CustomRoutingService } from 'src/app/_services/custom-routing.service';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import * as _ from 'lodash';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { ApiService } from 'src/app/_services/api.service';

@Component({
  selector: 'app-publish-cascade-models',
  templateUrl: './publish-cascade-models.component.html',
  styleUrls: ['./publish-cascade-models.component.scss']
})
export class PublishCascadeModelsComponent implements OnInit {

  trainingDataVolume: boolean;

  constructor(private customRouter: CustomRoutingService, private route: ActivatedRoute, private appUtilsService: AppUtilsService,
    private cascadeService: CascadeModelsService, private ns: NotificationService,
    private _deploymodelService: DeployModelService, private _localStorageService: LocalStorageService,
    private _notificationService: NotificationService,
    private router: Router, private _dialogService: DialogService, private _problemStatementService: ProblemStatementService,
    private _appUtilsService: AppUtilsService,
    private coreUtilService: CoreUtilsService, private _modalService: BsModalService,
    private uts: UsageTrackingService, private api: ApiService) { }

  cascadedId;
  deployedModel;
  modelName;
  linkedApps;
  modalRef: BsModalRef | null;
  modalRefPublicParent: BsModalRef;
  modelRefPublicChildModel: BsModalRef;


  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup'
  };

  datasource;
  useCase;
  correlationId;

  servicesToChoose = ['Link with web app', 'Create Web Service'];
  appsToDeploy = ['VDS', 'Guided Ticket Resolver(GTR)',
    'Automation Planner(AP)', 'Automation Journey(AJ)', 'Business Disruption Predictor(BDP)'];

  private apps = [
    { name: 'Virtual Data Scientist(VDS)', value: '1' },
    { name: 'Guided Ticket Resolver(GTR)', value: '2' },
    { name: 'Automation Planner(AP)', value: '3' },
    { name: 'Automation Journey(AJ)', value: '4' },
    { name: 'Business Disruption Predictor(BDP)', value: '5' }
  ];

  // variables to send in Post API
  saveselectedVersion = {};
  saveSelectedApps = [];
  deployappname: string;
  saveTypeOfDeployModel = {};
  //saveServiceType = {};
  saveTemplateName: String;
  newModelName: string;
  newCorrelationIdAfterCloned;
  modelsData;
  modelNames = [];
  trainedModels = [];
  modelAccurcy = [];
  selectedAccuracy: number;
  isPrivate = true;
  userID: string;
  isPopupClosed: boolean;
  webserviceLinkshow: boolean;
  templates;
  subscription: Subscription;
  clientUId: string;
  deliveryConstructUID: string;
  public browserRefresh: boolean;
  templatesCategories;
  isLoading = true;
  isPublisModelTab = false;
  accessRole: any;
  accessRoleName: string;
  accessRoleTitle: string;
  isPublicModelAccess = true;
  publicModelAccessColor: string;
  problemType: string;
  isTimeSeries = false;
  modelVesionArray: any = [];
  modelId = [];
  modelFrequency = [];
  selectedModelFrequency: string[] = [];
  deployedModelVersionName: string[];
  DeployedMVersion: string[] = [];
  wkAlertMsg: string;
  deployedFrequency: string[] = [];
  frequencyToReplace = [];
  showValidationMsg = false;
  disableAppToDeploy = false;
  checked = false;
  appname: string;
  checkAppsDeploye = {};
  errMsg: string;
  modelAccuracyObject = {};
  env;
  requestType;
  isInstaModel = false;
  isEnvironment = false;
  typeOfManagement = ['Program & Project Management',
    'Change Management'];
  deliveryTypeName;
  appsData: Array<any>;
  selectedAppId;
  readOnly;
  disableSave = false;
  isModelTemplate = false;
  isModelTemplateDataSource = false;
  isModelTemplateDisabled;
  openappwindow: string;
  isCustomCascadeModel = false;
  provideTrainingDataVolume: any;
  @Output() modelname: EventEmitter<any> = new EventEmitter();

  isOfflineSelected = false;
  isOnlineSelected = false;

  categoryText: string;
  categoryName;

  trainingFrequency: Array<any> = [
    // {
    //   name: 'Hourly', FrequencyVal: ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', '13', '14', '15',
    //     '16', '17', '18', '19', '20', '21', '22', '23']
    // },
    { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
    { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Fortnightly', FrequencyVal: ['1'] },
  ];

  trainingFrequencyValue: Array<any>;

  reTrainingFrequency: Array<any> = [
    { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
    { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Fortnightly', FrequencyVal: ['1'] },
  ];

  reTrainingFrequencyValue: Array<any>;

  retryAttemptValue = ['1', '2', '3'];

  selectedTFrequency = 'Frequency';
  selectedTFeqValues = 'Frequency Value';
  selectedTretryCount = 'Retry Attempt';
  tFrequency = '';

  selectedPFrequency = 'Frequency';
  selectedPFeqValues = 'Frequency Value';
  selectedPretryCount = 'Retry Attempt';
  pFrequency = '';

  selectedOfflineRetrain = false;
  selectedRFrequency = 'Frequency';
  selectedRFeqValues = 'Frequency Value';
  rFrequency = '';

  isPublicRadioSelected = false;
  ifSourceTypeIsFile = false;
  isTrainingFrequencyValueSelected = false;
  archiveModelDuration =  [{ value: '6', viewValue: '6 months' }, { value: '9', viewValue: '9 months' },
  { value: '12', viewValue: '12 months' }, { value: '15', viewValue: '15 months' }, { value: '18', viewValue: '18 months' },
  { value: '21', viewValue: '21 months' }, { value: '24', viewValue: '24 months' }, { value: '27', viewValue: '27 months' },
  { value: '30', viewValue: '30 months' }, { value: '33', viewValue: '33 months' }, { value: '36', viewValue: '36 months' }];
  selectedArchivalMonth = '6';

  ngOnInit() {
    this.publicModelAccessColor = 'transparent-color';
    this.env = sessionStorage.getItem('Environment');
    this.requestType = sessionStorage.getItem('RequestType');
    this.userID = this._appUtilsService.getCookies().UserId;
    if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
      this.appname = 'VDS(AIOPS)';
      this.isEnvironment = true;

      this.templatesCategories = [
        { 'name': 'Agile', 'value': 'Agile' },
        { 'name': 'Others', 'value': 'Others' },
      ];
    } else {
      this.appname = 'VDS(SI)';
      this.isEnvironment = false;

      this.templatesCategories = [{ 'name': 'System Integration', 'value': 'release Management' },
      { 'name': 'Agile', 'value': 'Agile' },
      { 'name': 'Devops', 'value': 'Devops' },
      { 'name': 'Others', 'value': 'Others' },
      { 'name': 'PPM', 'value': 'PPM' },
      ];
    }

    this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {

      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });
    this.cascadedId = sessionStorage.getItem('cascadedId');
    this.appUtilsService.loadingStarted();
    this.cascadeService.getCascadeDeployedModel(this.cascadedId).subscribe(response => {
      this.appUtilsService.loadingEnded();
      if (response.Status !== 'Deployed') {
        this.deployedModel = response;
        this.modelname.emit(response.ModelName);
        this.isModelTemplateDisabled = this.deployedModel.IsIngrainModel;
        this.setAttributes(this.deployedModel);
        this.fetchDeployDetails();
      } else {
        this.router.navigate(['/dashboard/problemstatement/cascadeModels/deployCascadeModels'], {});
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.ns.error(error.error);
    });
    this.getPublicTemplates();
  }

  next() {
    this.customRouter.redirectToNext();
  }


  previous() {
    this.customRouter.redirectToPrevious();
  }

  setAttributes(data) {
    this.datasource = data.DataSource;
    this.modelName = data.ModelName;
    this.useCase = data.BusinessProblems;
    this.problemType = data.ModelType;
    this.deliveryTypeName = data.Category;
    this.selectedAccuracy = data.CascadeModel[0].Accuracy;
    this.saveselectedVersion = data.CascadeModel[0].ModelVersion;
    this.correlationId = data.CascadeModel[0].CorrelationId;
    this.linkedApps = this.deployedModel.CascadeModel[0].LinkedApps;
    this.isCustomCascadeModel = this.deployedModel['IsCustomModel'];

    this.modelsData = data;

    this.saveSelectedApps = [];
    if (!this.coreUtilService.isNil(this.deployedModel.CascadeModel[0].LinkedApps)) {
      if (this.deployedModel.CascadeModel[0].LinkedApps.length > 0) {
        this.appname = this.deployedModel.CascadeModel[0].LinkedApps[0];
      }
    }
    //  this._appUtilsService.loadingEnded();
    this.getPublicTemplates();

    if (this.datasource.indexOf('.') > -1) {
      this.ifSourceTypeIsFile = true;
    }
  }

  getModels(trainedModels) {
    for (let i = 0; i < trainedModels.length; i++) {
      this.modelNames.push(trainedModels[i].modelName);
      this.modelFrequency.push(trainedModels[i].Frequency);

      if (trainedModels[i].r2ScoreVal) {
        if (trainedModels[i].r2ScoreVal.error_rate || trainedModels[i].r2ScoreVal.error_rate === 0) {
          this.modelAccurcy.push(trainedModels[i].r2ScoreVal.error_rate);
        }
      } else {
        this.modelAccurcy.push(trainedModels[i].Accuracy);
        this.modelAccuracyObject[trainedModels[i].modelName] = trainedModels[i].Accuracy;
      }
      if (trainedModels[i]._id) {
        this.modelId.push(trainedModels[i]._id);
      }
    }
  }

  /* selectedVersionOption(value) {
    this.saveselectedVersion = {};
    let counter = 0;
    this.selectedModelFrequency = [];
    this.modelVesionArray = value;

    if (this.isTimeSeries) {
      for (let version = 0; version < this.modelVesionArray.length; version++) {
        for (let a = 0; a < this.modelNames.length; a++) {
          if (this.modelVesionArray[version] === this.modelNames[a]) {

            counter = version;
            this.saveselectedVersion[counter] = {};
            this.saveselectedVersion[counter]['ModelName'] = this.modelVesionArray[version];

            if (this.modelAccurcy[a] === undefined) {
              this.saveselectedVersion[counter]['ModelAccuracy'] = 0.0;
            } else {
              this.saveselectedVersion[counter]['ModelAccuracy'] = Math.round(this.modelAccurcy[a] * 100) / 100;
            }
            this.saveselectedVersion[counter]['ModelId'] = this.modelId[a];
            this.saveselectedVersion[counter]['Frequency'] = this.modelFrequency[a];

            this.selectedModelFrequency.push(this.modelFrequency[a]);
          }
        }
        this.duplicateFrequency(this.selectedModelFrequency[version]);
      }
      if (this.errMsg) {
        this._notificationService.error(this.errMsg);
      }

    } else {
      this.saveselectedVersion = value;
      if (!this.isTimeSeries && this.problemType !== 'Regression') {
        this.selectedAccuracy = this.modelAccuracyObject[value];
      } else {
        for (let m = 0; m < this.modelNames.length; m++) {
          for (let a = m; a < this.modelAccurcy.length; a++) {
            if (value === this.modelNames[a]) {
              this.selectedAccuracy = this.modelAccurcy[a];
            }
          }
        }
      }
      this.selectedAccuracy = Math.round(this.selectedAccuracy * 100) / 100;
    }
  } */

  /* duplicateFrequency(item) {
    let counter = 0;
    for (const freq in this.modelVesionArray) {
      if (this.modelVesionArray[freq].indexOf(item) >= 0) {
        if (counter === 0) {
          this.errMsg = '';
        } else {
          this.errMsg = 'One ' + item + ' Frequency Already Selected, Please Select Other Frequency';
          break;
        }
        counter = counter + 1;
      }
    }
  } */


  getPublicTemplates() {
    if (this.deliveryTypeName === 'release Management' || this.deliveryTypeName === 'AD') {
      this.categoryName = 'AD';
      this.categoryText = 'System Integration'
    } else if (this.deliveryTypeName === 'Agile Delivery' || this.deliveryTypeName === 'Agile') {
      this.categoryName = 'Agile';
      this.categoryText = 'Agile';
    } else if (this.deliveryTypeName === 'Devops') {
      this.categoryName = 'Devops';
      this.categoryText = 'Devops';
    } else if (this.deliveryTypeName === 'AIops' || this.deliveryTypeName === 'AM' || this.deliveryTypeName === 'IO') {
      this.categoryName = 'AIops';
      this.categoryText = 'AIops';
    } else if (this.deliveryTypeName === 'PPM') {
      this.categoryName = 'PPM';
      this.categoryText = 'PPM';
    } else {
      this.categoryName = 'Others';
      this.categoryText = 'Others';
    }
  }


  // rodioSelectedIsPublic(elementRef, publicParentTemplate: TemplateRef<any>) {
  rodioSelectedIsPublic(elementRef) {
    this.selectedOfflineRetrain = false;
    this.isOfflineSelected = false;
    this.isOnlineSelected = false;
    this.resetValuesOfflineOnline();
    if (elementRef.checked === true) {
      this.isPublicRadioSelected = true;
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        this.appname = 'VDS(AIOPS)';
      } else {
        this.appname = 'VDS(SI)';
      }
      this.changeapp(this.appname);
      this.saveTypeOfDeployModel = {};
      this.isPrivate = false;
      this.isModelTemplate = false;
      this.saveTypeOfDeployModel['Public'] = 'True';
      this.webserviceLinkshow = false;
      this.trainingDataVolume = false;
      if (this.saveSelectedApps[0] === undefined) {
        this.saveSelectedApps = [];
        this.appsData.forEach(appvalue => {
          if (appvalue.ApplicationName === 'Ingrain') {
            this.saveSelectedApps.push('Ingrain');
          }
        });
      }
      // this.modalRefPublicParent = this._modalService.show(publicParentTemplate, { class: 'deploymodle-publicparent' });
    }
  }

  rodioSelectedIsPrivate(elementRef) {
    this.selectedOfflineRetrain = false;
    this.isOfflineSelected = false;
    this.isOnlineSelected = false;
    this.resetValuesOfflineOnline();
    if (elementRef.checked === true) {
      this.saveSelectedApps = [];
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        this.appname = 'VDS(AIOPS)';
      } else {
        this.appname = 'VDS(SI)';
      }
      this.changeapp(this.appname);
      this.isPublicRadioSelected = false;
      this.isPrivate = true;
      this.isModelTemplate = false;
      this.saveTypeOfDeployModel = {};
      this.webserviceLinkshow = false;
      this.trainingDataVolume = false;
      this.saveTypeOfDeployModel['Private'] = 'True';
      if (this.saveSelectedApps[0] === undefined) {
        this.appsData.forEach(appvalue => {
          if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            if (appvalue.ApplicationName === 'VDS(AIOPS)') {
              this.saveSelectedApps[0] = 'VDS(AIOPS)';
            }
          } else {
            this.saveSelectedApps[0] = 'VDS(SI)';
          }
        });
      }
    }
  }

  isSpecialCharacter(input) {
    const regex = /^[0-9 ]+$/;
    //console.log(input);
    const isValid = regex.test(input);
    this.provideTrainingDataVolume = input;
    if (input && input.length > 0) {
      if (!isValid) {
        this._notificationService.error('Enter only numeric value');
        return 0;
      } else {
        if (input < 4) {
          this._notificationService.error('The min limit on data pull is 4 records. Please validate the value.');
          return 1;
        }
        else if (input > 30000) {
          this._notificationService.error('The max limit on data pull is 30000 records. Please validate the value.');
        }
      }
    }
  }

  deployModel() {
    if (this.isPrivate === true) {
      if (this.saveSelectedApps[0] === undefined) {
        this.saveSelectedApps = [];
        this.saveTypeOfDeployModel = {};
        this.saveTypeOfDeployModel['Private'] = 'True';
        this.appsData.forEach(appvalue => {
          if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
            if (appvalue.ApplicationName === 'VDS(AIOPS)') {
              this.saveSelectedApps[0] = 'VDS(AIOPS)';
            }
          } else {
            this.saveSelectedApps[0] = 'VDS(SI)';
          }
        });
      }
    }

    if (this.saveSelectedApps.length === 0) {
      this._notificationService.error('Please choose apps to deploy');
    }
    if (this.saveSelectedApps.length !== 0) {
      // this.provideTrainingDataVolume = null;
      if (this.provideTrainingDataVolume === undefined && this.isModelTemplate === true) {
        this._notificationService.error('Please provide training data volume');
      } else {
        this.postDeployModelCall();
      }
      // this.postDeployModelCall();
    }

    //   if (this.isTimeSeries){
    //   if (!this.provideTrainingDataVolume && this.isModelTemplate === true){
    //     this._notificationService.error('Please provide training data volume');
    //   }
    //   else {
    //    // this.postDeployModelCall();
    //   }
    // }
  }

  // call post api of deploy model this.saveServiceType, this.selectedAccuracy
  postDeployModelCall() {
    let retryKey = 'RetryCount';
    let trainingObj = {};
    let predictionObj = {};
    let retraining = {};

    if (this.selectedTFrequency !== 'Frequency') {
      trainingObj[this.tFrequency] = this.selectedTFeqValues;
      trainingObj[retryKey] = this.selectedTretryCount;
    }

    if (this.selectedPFrequency !== 'Frequency') {
      predictionObj[this.pFrequency] = this.selectedPFeqValues;
      predictionObj[retryKey] = this.selectedPretryCount;
    }

    if (this.selectedRFrequency !== 'Frequency') {
      retraining[this.rFrequency] = this.selectedRFeqValues;
    }

    if (this.isPrivate) {
      this.isOfflineSelected = false;
    }

    this._deploymodelService.postDeployModels(this.correlationId, this.saveselectedVersion, this.saveSelectedApps,
      this.saveTypeOfDeployModel, this.selectedAccuracy, this.isPrivate, this.saveTemplateName,
      this.userID, this.modelName, this.problemType, this.frequencyToReplace, this.selectedAppId, this.isModelTemplate,
      this.provideTrainingDataVolume, this.isOfflineSelected, this.isOnlineSelected, this.selectedOfflineRetrain,
      trainingObj, predictionObj, retraining, this.selectedArchivalMonth).subscribe(data => {
        if (data) {
          console.log(data);
          this._notificationService.success('Model Deployed Successfully');
          this.router.navigate(['dashboard/problemstatement/cascadeModels/deployCascadeModels'],
            {});
        }
      });
  }

  onSelectedTemplate(elementRef, template) {
    if (elementRef.checked === true) {
      this.saveTemplateName = template
      this._deploymodelService.setTemplateName(template);
    }
  }

  fetchDeployDetails() {
    this._problemStatementService.getDeployAppsDetails(this.correlationId, this.clientUId, this.deliveryConstructUID).subscribe(data => {
      this.appsData = data;
      this.changeapp(this.appname);
      // console.log(this.appsData);
    });
  }

  fetchApp(data) {
    if (data === 'Success') {
      this.fetchDeployDetails();
    }
  }

  changeapp(appname) {
    this.saveSelectedApps = [];
    this.saveSelectedApps.push(appname);
    const appId = this.appsData.filter(model => model.ApplicationName === appname);
    this.selectedAppId = (appId.length > 0) ? appId[0].ApplicationId : '';
  }

  rodioSelectedIsModelTemplate(elementRef) {
    this.selectedOfflineRetrain = false;
    this.isOfflineSelected = false;
    this.isOnlineSelected = false;
    this.resetValuesOfflineOnline();
    if (elementRef.checked === true) {
      if ((this.env === 'PAM' || this.env === 'FDS') && (this.requestType === 'AM' || this.requestType === 'IO')) {
        this.appname = 'VDS(AIOPS)';
      } else {
        this.appname = 'VDS(SI)';
      }
      this.saveTypeOfDeployModel = {};
      this.isPublicRadioSelected = false;
      this.isPrivate = false;
      this.isModelTemplate = true;
      this.saveTypeOfDeployModel['ModelTemplate'] = 'True';
      this.webserviceLinkshow = true;
      this.trainingDataVolume = true;
      if (this.saveSelectedApps[0] === undefined) {
        this.saveSelectedApps = [];
        this.appsData.forEach(appvalue => {
          if (appvalue.ApplicationName === 'Ingrain') {
            this.saveSelectedApps.push('Ingrain');
          }
        });
      }
      // this.modalRefPublicParent = this._modalService.show(publicParentTemplate, { class: 'deploymodle-publicparent' });
    }
  }

  closeWebSerciveURLWindow() {
    this.webserviceLinkshow = false;
    this.isPopupClosed = true;
    return this.webserviceLinkshow;
  }

  selectedCategories(value) {
    // this.categoryName = value;
    for (let a in this.templatesCategories) {
      if (this.templatesCategories[a].name === value) {
        this.saveTemplateName = this.templatesCategories[a].value;
      }
    }
  }

  rodioSelectedIsOffline(elementRef) {
    if (elementRef.checked === true) {
      this.resetValuesOfflineOnline();
      this.isOfflineSelected = true;
      this.isOnlineSelected = false;
      this.isModelTemplate = true;
    }
  }

  rodioSelectedIsOnline(elementRef) {
    if (elementRef.checked === true) {
      this.resetValuesOfflineOnline();
      this.isOfflineSelected = false;
      this.isOnlineSelected = true;
      this.isModelTemplate = true;
      this.selectedOfflineRetrain = true;
      this.isTrainingFrequencyValueSelected = false;
    }
  }

  selectedTrainingFrequency(value) {
    this.selectedTFrequency = value;
    this.tFrequency = value.name;
    this.trainingFrequencyValue = this.trainingFrequency.find(feq => feq.name == value.name).FrequencyVal;
  }

  selectedTFeqValue(value) {
    this.selectedTFeqValues = value;
  }

  selectedTRetryAttempt(value) {
    this.selectedTretryCount = value;
  }

  selectedPredictionFrequency(pValue) {
    this.selectedPFrequency = pValue;
    this.pFrequency = pValue.name;
    this.trainingFrequencyValue = this.trainingFrequency.find(feq => feq.name == pValue.name).FrequencyVal;
  }

  selectedPFeqValue(pValue) {
    this.selectedPFeqValues = pValue;
  }

  selectedPRetryAttempt(value) {
    this.selectedPretryCount = value;
  }

  checkboxSelectedIsOffline(elementRef) {
    if (elementRef.checked === true) {
      this.selectedOfflineRetrain = true;
    } else {
      this.selectedOfflineRetrain = false;
      this.resetValuesOfflineOnline();
    }
  }

  selectedRetrainingFrequency(rValue) {
    this.selectedRFrequency = rValue;
    this.rFrequency = rValue.name;
    this.reTrainingFrequencyValue = this.reTrainingFrequency.find(feq => feq.name == rValue.name).FrequencyVal;
    this.isTrainingFrequencyValueSelected = true;
  }

  selectedRFeqValue(pValue) {
    this.selectedRFeqValues = pValue;
  }

  resetValuesOfflineOnline() {
    this.selectedTFrequency = 'Frequency';
    this.selectedTFeqValues = 'Frequency Value';
    this.selectedTretryCount = 'Retry Attempt';

    this.selectedPFrequency = 'Frequency';
    this.selectedPFeqValues = 'Frequency Value';
    this.selectedPretryCount = 'Retry Attempt';

    this.selectedRFrequency = 'Frequency';
    this.selectedRFeqValues = 'Frequency Value';

    this.selectedOfflineRetrain = false;

    this.rFrequency = '';
    this.isTrainingFrequencyValueSelected = false;
  }

  selectArchivalDuration(archivalMonths) {
    this.selectedArchivalMonth = archivalMonths;
    // console.log('test----', this.selectedArchivalMonth)
  }
}
