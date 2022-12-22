import { Injectable } from '@angular/core';
import { ApiCore } from './api-core.service';

const API_KEY_PARAMS = {
  ClientUID: 'ClientUID',
  DeliveryConstructUID: 'DeliveryConstructUID',
  CreatedByUser: 'CreatedByUser',
  UserId: 'UserId',
  ProblemType: 'ProblemType',
  DeliveryTypeName: 'DeliveryTypeName',
  DeliveryTypeID: 'DeliveryTypeID',
  UseCaseDescription: 'UseCaseDescription',
  UseCaseName: 'UseCaseName',
  Generic: 'Generic',
  TargetColumn: 'TargetColumn',
  UseCaseID: 'UseCaseID'
};


@Injectable({
  providedIn: 'root'
})
export class PayloadHelper {
  // public paramData = {} as IRequestPayload;
  constructor(private apiCore: ApiCore) {
  }

  getFileUploadPayload() {
   const comman = this.getClientIdDCIDUForFileUpload();

   const data = {};
   data[API_KEY_PARAMS.ProblemType] = this.apiCore.paramData.problemType;
   data[API_KEY_PARAMS.DeliveryTypeName] = this.apiCore.paramData.vdsCategoryType;
   data[API_KEY_PARAMS.DeliveryTypeID] = this.apiCore.paramData.deliveryConstructUId ? this.apiCore.paramData.deliveryConstructUId : sessionStorage.getItem('DeliveryConstructUId');
   data[API_KEY_PARAMS.UseCaseName] = this.apiCore.paramData.vdsUsecaseName;
   data[API_KEY_PARAMS.UseCaseDescription] = this.apiCore.paramData.vdsUsecasedesc;
  //  data[API_KEY_PARAMS.UseCaseID] = this.apiCore.paramData.UseCaseID; // file upload UseCaseId
   if (this.apiCore.paramData.problemType !== API_KEY_PARAMS.Generic) {
   data[API_KEY_PARAMS.TargetColumn] = 'Defect';
   data[API_KEY_PARAMS.UseCaseDescription] = 'ADSP';
   }

  Object.assign(data, comman);
  return data;
  }

  getClientIdDCIDUserName() {
    const data = {};
    data[API_KEY_PARAMS.ClientUID] = this.apiCore.paramData.clientUID ? this.apiCore.paramData.clientUID : sessionStorage.getItem('ClientUId');
    data[API_KEY_PARAMS.DeliveryConstructUID] = this.apiCore.paramData.deliveryConstructUId ? this.apiCore.paramData.deliveryConstructUId : sessionStorage.getItem('DeliveryConstructUId');
    data[API_KEY_PARAMS.UserId] = this.apiCore.getUserId() ? this.apiCore.getUserId() : sessionStorage.getItem('userId');
    return data;
  }

  getClientIdDCIDUForFileUpload() {
    const data = {};
    data[API_KEY_PARAMS.ClientUID] = this.apiCore.paramData.clientUID ? this.apiCore.paramData.clientUID : sessionStorage.getItem('ClientUId');
    data[API_KEY_PARAMS.DeliveryConstructUID] = this.apiCore.paramData.deliveryConstructUId ? this.apiCore.paramData.deliveryConstructUId : sessionStorage.getItem('DeliveryConstructUId');
    data[API_KEY_PARAMS.CreatedByUser] = this.apiCore.getUserId() ? this.apiCore.getUserId() : sessionStorage.getItem('userId');
    return data;
  }


}
