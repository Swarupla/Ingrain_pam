import { Component, OnInit } from '@angular/core';
import { DialogService } from 'src/app/dialog/dialog.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { throwError } from 'rxjs';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { BsModalService } from 'ngx-bootstrap/modal';
import { CustomConfigComponent } from '../dashboard/deploy-model/custom-config/custom-config.component';
import { ServiceTypes} from '../../_enums/service-types.enum';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';


@Component({
  selector: 'app-template-name-modal',
  templateUrl: './template-name-modal.component.html',
  styleUrls: ['./template-name-modal.component.scss']
})
export class TemplateNameModalComponent implements OnInit {
  modalName = '';
  entityNameInput = '';
  title = '';
  pageInfo = 'CreateModel';
  publishAIServiceUseCase = false;
  usecaseName: string;
  usecaseDecription: string;
  applicationName: string;
  appsData = [];
  saveSelectedApps = [];
  disableSave = false;
  source = '';

  isOfflineSelected = false;
  isOnlineSelected = false;
  selectedOfflineRetrain = false;
  rFrequency = '';
  showTrainingDetails = false;

  trainingFrequency: Array<any> = [
    { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
    { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Fortnightly', FrequencyVal: ['1'] },
  ];

  trainingFrequencyValue: Array<any>;
  predictionFrequencyValues: Array<any>;

  reTrainingFrequency: Array<any> = [
    { name: 'Daily', FrequencyVal: ['1', '2', '3', '4', '5', '6'] },
    { name: 'Weekly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Monthly', FrequencyVal: ['1', '2', '3'] },
    { name: 'Fortnightly', FrequencyVal: ['1'] },
  ];

  reTrainingFrequencyValue: Array<any>;

  retryAttemptValue = ['1', '2', '3'];

  pFrequency = '';
  selectedRFrequency = 'Frequency';
  selectedRFeqValues = 'Frequency Value';
  selectedTFrequency = 'Frequency';
  tFrequency = '';
  offlineTrainingValueSelection = { 'trainingValue': false, 'trainingFrequencyValue': false, 'trainingRetryValue': false };
  selectedTFeqValues = 'Frequency Value';
  selectedTretryCount = 'Retry Attempt';
  selectedPFrequency = 'Frequency';
  offlinePredictionValueSelection = { 'predictionValue': false, 'predictionFrequencyValue': false, 'predictionRetryValue': false };
  selectedPFeqValues = 'Frequency Value';
  selectedPretryCount = 'Retry Attempt';
  isOnlineValid = false;
  isOfflineValid = false;
  isWordCloud = false;
  serviceType = ServiceTypes;
  ConfigStorageKey = 'CustomConfiguration';
  DataSource : string;
  isCustomConstraintsEnabled : boolean = false;

  constructor(private dr: DialogRef, private dc: DialogConfig, private ns: NotificationService,
    private pb: ProblemStatementService, private ls: LocalStorageService, private cus: CoreUtilsService,
    private _usageTrackingService: UsageTrackingService, private _modalService : BsModalService,
    private environmentService : EnvironmentService) { }

  ngOnInit() {

    if (this.environmentService.IsPADEnvironment()) {//Enable custom constraints option only for PAD environmant.
      this.isCustomConstraintsEnabled = true;
    }

    this.title = this.dc.data ? (this.dc.data['title'] ? this.dc.data['title'] : 'Enter Model Name') : 'Enter Model Name';
    this.isWordCloud = (this.dc.data && this.dc.data['isWordCloud']) ? this.dc.data['isWordCloud'] : false;
    if (this.dc.data && this.dc.data.hasOwnProperty('pageInfo')) {
      this.pageInfo = this.dc.data['pageInfo'];
    }
    if (this.dc.data && this.dc.data.hasOwnProperty('source')) {
      this.source = this.dc.data['source'];
    }
    if(this.dc.data && this.dc.data.hasOwnProperty('DataSource')){
      this.DataSource = this.dc.data['DataSource'];
    }
    if (this.title === 'Enter UseCase Name') {
      this.publishAIServiceUseCase = true;

      this.pb.getDeployAppsDetails(this.dc.data.correlationid, this.dc.data.clientId, this.dc.data.deliveryConstructId).subscribe(data => {
        this.appsData = data;
        // console.log('app-----------', this.appsData);
      });

    } else {
      this.publishAIServiceUseCase = false;
    }
  }

  onClose() {
    this.dr.close();
    this.resetOfflineOnlineValues();
  }

  onEnter(modalName, entityNameInput?) {
    this._usageTrackingService.usageTracking('Create Custom Model', 'Models Created');

    this.disableSave = true;
    const valid = this.cus.isSpecialCharacter(this.modalName);
    if (valid === 0) {
      this.disableSave = false;
      return 0;
    } else {
      this.modalName = modalName.model;
      if (modalName.valid) {
        if (this.pageInfo === 'CreateModel') {
          this.pb.getExistingModelName(modalName.model).subscribe(message => {
            if (message === false) {
              this.setModelNameinLocalStorage(this.modalName);
              this.dr.close(this.modalName);
            } else {
              this.disableSave = false;
              message = 'The model name already exists. Choose a different name.';
              throwError(message);
              this.ns.error(message);
            }
          }, error => {
            if (error.status === 400) {
              this.disableSave = false;
              this.ns.error('Special Character are not allowed');
            } else {
              this.disableSave = false;
              throwError(error); this.ns.error('Something went wrong.');
            }
          });
        } else if (this.pageInfo === 'InferenceEngine') {
          this.dr.close({ 'ModelName': this.modalName, 'EntityName': this.entityNameInput })
        }
        else {
          this.dr.close(this.modalName);
        }
      }
      if (modalName.invalid && modalName.touched) {
        this.disableSave = false;
        throwError('Enter Model Name');
        this.ns.error('Enter Model Name');
      }
      if (modalName.pristine) {
        this.disableSave = false;
        throwError('Enter Model Name');
        this.ns.error('Enter Model Name');
      }
    }
  }

  setModelNameinLocalStorage(modelName) {
    this.ls.setLocalStorageData('modelName', modelName);
  }

  getValidation() {
    if (this.isOnlineSelected) {
      this.isOnlineValid = this.selectedOfflineRetrain && this.rFrequency.length > 0 && this.selectedRFeqValues !== 'Frequency Value';
    }
    if (this.isOfflineSelected) {
      this.isOfflineValid = (this.selectedOfflineRetrain ? this.rFrequency.length > 0 && this.selectedRFeqValues !== 'Frequency Value' : true) && this.tFrequency.length > 0 && this.selectedTFeqValues !== 'Frequency Value' && this.selectedTretryCount !== 'Retry Attempt' && this.pFrequency.length > 0 && this.selectedPFeqValues !== 'Frequency Value' && this.selectedPretryCount !== 'Retry Attempt';
    }
  }

  resetOfflineOnlineValues() {
    this.rFrequency = '';
    this.selectedRFeqValues = 'Frequency Value';
    this.selectedOfflineRetrain = false;
    this.tFrequency = '';
    this.selectedTFeqValues = 'Frequency Value';
    this.selectedTretryCount = 'Retry Attempt';
    this.pFrequency = '';
    this.selectedPFeqValues = 'Frequency Value';
    this.selectedPretryCount = 'Retry Attempt';
    this.selectedRFrequency = 'Frequency';
    this.selectedTFrequency = 'Frequency';
    this.selectedPFrequency = 'Frequency';
    this.reTrainingFrequencyValue = Array<any>();
    this.trainingFrequencyValue = Array<any>();
    this.predictionFrequencyValues = Array<any>();
  }

  radioSelectedIsOffline(elementRef) {
    this.showTrainingDetails = true;
    if (elementRef.checked === true) {
      this.isOfflineSelected = true;
      this.isOnlineSelected = false;
    }
    this.resetOfflineOnlineValues();
    this.getValidation();
  }

  radioSelectedIsOnline(elementRef) {
    this.showTrainingDetails = true;
    if (elementRef.checked === true) {
      this.isOnlineSelected = true;
      this.isOfflineSelected = false;
    }
    this.resetOfflineOnlineValues();
    this.getValidation();
  }

  checkboxSelectedIsOffline(elementRef) {
    this.selectedRFrequency = 'Frequency';
    this.selectedRFeqValues = 'Frequency Value';
    if (elementRef.checked === true) {
      this.selectedOfflineRetrain = true;
    } else {
      this.selectedOfflineRetrain = false;
    }
    this.getValidation();
  }

  selectedRetrainingFrequency(rValue) {
    this.selectedRFrequency = rValue;
    this.rFrequency = rValue.name;
    this.reTrainingFrequencyValue = this.reTrainingFrequency.find(feq => feq.name == rValue.name).FrequencyVal;
    this.getValidation();
  }

  selectedRFeqValue(pValue) {
    this.selectedRFeqValues = pValue;
    this.getValidation();
  }

  selectedTrainingFrequency(selectedTFrequency) {
    this.selectedTFrequency = selectedTFrequency;
    this.tFrequency = selectedTFrequency.name;
    this.offlineTrainingValueSelection.trainingValue = this.tFrequency ? true : false;
    this.trainingFrequencyValue = this.trainingFrequency.find(feq => feq.name == selectedTFrequency.name).FrequencyVal;
    this.getValidation();
  }

  selectedTFeqValue(selectedTFeqValues) {
    this.selectedTFeqValues = selectedTFeqValues;
    this.offlineTrainingValueSelection.trainingFrequencyValue = this.selectedTFeqValues ? true : false;
    this.getValidation();
  }

  selectedTRetryAttempt(value) {
    this.selectedTretryCount = value;
    this.offlineTrainingValueSelection.trainingRetryValue = this.selectedTretryCount ? true : false;
    this.getValidation();
  }

  selectedPredictionFrequency(pValue) {
    this.selectedPFrequency = pValue;
    this.pFrequency = pValue.name;
    this.offlinePredictionValueSelection.predictionValue = this.pFrequency ? true : false;
    this.predictionFrequencyValues = this.trainingFrequency.find(feq => feq.name == pValue.name).FrequencyVal;
    this.getValidation();
  }

  selectedPFeqValue(pValue) {
    this.selectedPFeqValues = pValue;
    this.offlinePredictionValueSelection.predictionFrequencyValue = this.selectedPFeqValues ? true : false;
    this.getValidation();
  }

  selectedPRetryAttempt(value) {
    this.selectedPretryCount = value;
    this.offlinePredictionValueSelection.predictionRetryValue = this.selectedPretryCount ? true : false;
    this.getValidation();
  }

  /**
   * Create usecase from AI core services
   * @param usecaseName
   * @param usecaseDecription
   * @param applicationName
   */

  createUseCase(usecaseName, usecaseDecription, applicationName) {
    if (usecaseName.model === undefined) {
      this.ns.warning('Please enter usecase name');
      return 0;
    } if (usecaseDecription.model === undefined) {
      this.ns.warning('Please enter usecase description');
      return 0;
    } if (applicationName.model === undefined) {
      this.ns.warning('Please select application name');
      return 0;
    }
    if (this.isSpecialCharacter(usecaseName.model + '' + usecaseDecription.model) === 0) {
      return 0;
    }

    const result = {
      'UsecaseName': usecaseName.model,
      'Description': usecaseDecription.model,
      'ApplicationName': applicationName.model,
      'ApplicationId': this.saveSelectedApps[0].ApplicationId
    };
    if (this.publishAIServiceUseCase) {
      result['IsCarryOutRetraining'] = this.selectedOfflineRetrain;
      result['IsOnline'] = this.isOnlineSelected;
      result['IsOffline'] = this.isOfflineSelected;
      result['Retraining'] = { [this.rFrequency]: this.selectedRFeqValues };
      if (this.isOfflineSelected) {
        result['Training'] = { [this.tFrequency]: this.selectedTFeqValues, 'RetryCount':  this.selectedTretryCount };
        result['Prediction'] = { [this.pFrequency]: this.selectedPFeqValues, 'RetryCount':  this.selectedPretryCount };
      } else {
        result['Training'] = {};
        result['Prediction'] = {};
      }
    }

    this.dr.close(result);
  }

  changeapp(appname) {
    this.saveSelectedApps = [];
    this.saveSelectedApps = this.appsData.filter(model => model.ApplicationName === appname);
  }

  isSpecialCharacter(input: string) {
    const regex = /^[-A-Za-z0-9., ]+$/;
    if (input && input.length > 0) {
      const isValid = regex.test(input);
      if (!isValid) {
        this.ns.warning('No special characters allowed.');
        return 0; // Return 0 , if input string contains special character
      } else {
        return 1; // Return 1 , if input string does not contains special character
      }
    }
  }

    // code to show custom configuration Region - starts.
    showCustomConfiguration(selectedService) {
      this._modalService.show(CustomConfigComponent, { class: 'modal-dialog modal-lg modal-dialog-centered', backdrop: 'static', initialState: { title: 'Custom Configuration', dataSource: this.DataSource, selectedService: selectedService, serviceLevel : 'AI' } }).content.customConfig.subscribe(customConfig => {
        const configKey = `${selectedService}${this.ConfigStorageKey}`;
        if(customConfig.length > 0){
          sessionStorage.setItem(configKey, JSON.stringify(customConfig));
        }else{
          sessionStorage.removeItem(configKey); 
        }
      });
    }
    // code to show custom configuration Region - ends.

}
