import { Injectable } from '@angular/core';
import { AlertService } from './alert-service.service';

interface IReferenceData {
  clientUID: any;
  parentUID: any;
  deliveryConstructUId: any;
  vdsCategoryType: any;
  vdsUsecaseName: any;
  vdsUsecasedesc: any;
  problemType: any;
  targetcolumn: any;
  selectInputId;
  selectSimulationId;
  UseCaseID; 
  SelectedCurrentRelease;
  TeamCheckedBox;
  InputSelection;
  unSelectedPhases;
  viewSimulationFlag;
  
}


@Injectable({
  providedIn: 'root'
})
export class ApiCore {
  public paramData = {} as IReferenceData;
  constructor(private mesage: AlertService) {
  }

  setClientUID(value?) {
    // sessionStorage.setItem('clientID', data.ClientUId);
    // sessionStorage.setItem('dcID', data.DeliveryConstructUID);
    this.paramData.clientUID = sessionStorage.getItem('clientID') ? sessionStorage.getItem('clientID') : sessionStorage.getItem('ClientUId'); // '00100000-0000-0000-0000-000000000000';
  }

  setDeliveryConstructUID(value?) {
    this.paramData.deliveryConstructUId = sessionStorage.getItem('dcID') ? sessionStorage.getItem('dcID') : sessionStorage.getItem('DeliveryConstructUId');
  }

  setVDSCategoryType(value?) {
    this.paramData.vdsCategoryType = sessionStorage.getItem('VDSCategoryType');
  }

  setVDSUseCaseName(value?) {
    // new use case created flow // existing generic flow
    this.paramData.vdsUsecaseName = sessionStorage.getItem('VDSUseCaseName');
  }

  setVDSUseCaseDesc(value?) {
    this.paramData.vdsUsecasedesc = '';
  }

  setVDSProblemType(value?) {
    // sessionStorage.setItem('VDSProblemType', 'ADSP');
    // sessionStorage.setItem('VDSUseCaseName', 'ADSP');
    this.paramData.problemType = sessionStorage.getItem('VDSProblemType');
  }

  setUseCaseID(value?) {
  this.paramData.UseCaseID = value;
  }

  getUserId() {
    return sessionStorage.getItem('userId');
  }

  getSimulationOutput(inputVersion) {
    // return
  }

  setCoreParams() {
    this.setClientUID();
    this.setDeliveryConstructUID();
    this.setVDSProblemType();
    // this.setVDSRequestType();
    this.setVDSCategoryType();
    this.setVDSUseCaseDesc();
    this.setVDSUseCaseName();
  }

  isSpecialCharacter(input: string) {
    const regex = /^[-A-Za-z0-9., ]+$/;
    if (input && input.length > 0) {
      const isValid = regex.test(input);
      if (!isValid) {
        this.mesage.warning('No special characters allowed.');
        return 0; // Return 0 , if input string contains special character
      } else {
        return 1; // Return 1 , if input string does not contains special character
      }
    }
  }

  isSpecialCharacterGeneric(input: string) {
    const regex = /^[A-Za-z0-9]+$/;
    if (input && input.length > 0) {
      const isValid = regex.test(input);
      if (!isValid) {
        this.mesage.warning('No special characters allowed.');
        return 0; // Return 0 , if input string contains special character
      } else {
        return 1; // Return 1 , if input string does not contains special character
      }
    }
  }
}
