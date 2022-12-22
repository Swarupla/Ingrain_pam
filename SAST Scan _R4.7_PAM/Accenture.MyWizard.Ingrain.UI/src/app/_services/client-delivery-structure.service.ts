import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ClientDeliveryStructureService {

  constructor(private api: ApiService) { }
  getUserStoryclientStructures(ClientUID, UserEmail) {
    // return this.api.get('UserStoryclientStructures', { 'ClientUID': ClientUID, 'UserEmail': UserEmail });

    return this.api.get('UserStoryclientStructuresdetails', { 'ClientUID': '00000000-0000-0000-0000-000000000000', 'UserEmail': UserEmail });
  }
  getDeliveryConstructName(ClientUID, DcUid, UserEmail) {
    return this.api.get('DeliveryConstructName', { 'ClientUID': ClientUID, 'UserEmail': UserEmail, 'DeliveryConstructUId': DcUid });
  }

  getSearchDeliveryClientName(ClientUID, DcUid, UserEmail, SearchText) {
    return this.api.get('GetAccountClientDCSearch', { 'ClientUId': ClientUID, 'DeliveryConstructUId': DcUid, 'Email': UserEmail, 'SearchStr': SearchText });
  }
  GetDeliveryStructsForClient(ClientUID, DcUid, UserEmail, token) {
    const postData = {
      'ClientUID': ClientUID,
      'DeliveryConstructUId': DcUid,
      'UserID': UserEmail,
      'Token': token
    };
    return this.api.post('GetDemographicTreeForUser', postData);
  }
  getDeliveryConstructs(UserId) {
    return this.api.get('GetDeliveryConstructs', { 'UserId': UserId });
  }
  postDeliveryConstruct(postData) {
    return this.api.post('PostDeliveryConstruct', postData);
  }
  getPriviligesForAccountorUser(clientUID, DeliveryConstructUId, userID, token) {
    const data = {
      ClientUID: clientUID,
      DeliveryConstructUId: DeliveryConstructUId,
      userID: userID,
      Token: token
    };

    return this.api.post('PrivilegesForAccountorUser', data);
  }
  getClientName(ClientUID, DeliveryConstructUId, UserID) {
    return this.api.get('ClientNameByClientUId', { 'ClientUID': ClientUID ,
    'DeliveryConstructUId': DeliveryConstructUId, 'UserId': UserID });
  }

  getPamDeliveryConstructName(DeliveryConstructUId) {
    return this.api.get('PamDeliveryConstructName', { 'DeliveryConstructUId': DeliveryConstructUId });
  }

  getAppExecutionContext(clientUID, deliveryConstructUId, UserId) {
    // return this.api.getDynamicData('appExecutionContext', { 'clientUId': clientUID, 'deliveryConstructUId': deliveryConstructUId });
    return this.api.get('GetAppExecutionContext', { 'clientUId': clientUID, 'deliveryConstructUId': deliveryConstructUId,
    'UserEmail': UserId });
  }

  getClientDetails(clientUID, deliveryConstructUId, UserEmail) {
    // return this.api.getDynamicData('AccountClients', { 'clientUId': clientUID, 'deliveryConstructUId': deliveryConstructUId });
    return this.api.get('GetClientDetails', { 'clientUId': clientUID, 'deliveryConstructUId': deliveryConstructUId,
     'UserEmail': UserEmail });
  }

  getPheonixTokenForVirtualAgent() {
    return this.api.get('PheonixTokenForVirtualAgent', { });
  }

  getPheonixAccountDetail() {
    return this.api.getPheonix('/v1/Account', { });
  }

  updateAccountPrivacy(clientUId, deliveryConstructUId, payload) {
    return this.api.postPheonix('/v1/UpdateAccountPrivacy', payload, { 'clientUId': clientUId,
    'deliveryConstructUId': deliveryConstructUId });
  }
  getAboutUsReleaseDetail(clientUId, UserEmail) {
    return this.api.get('AppBuildInfo', {'ClientUID': clientUId, 'UserEmail': UserEmail});
  }

  getDemographicsName(params) {
    return this.api.get('GetDemographicsName', params);
  }

  getClientScopeSelector(params) {
    return this.api.get('GetClientScopeSelector', params);
  }

  getLanguage(clientUID, deliveryConstructUId, UserEmail) {
    return this.api.get('multilanguage', { 'ClientUId': clientUID,
      'DeliveryConstructUId': deliveryConstructUId, 'UserEmail': UserEmail });
  }


  getVideoPlayer() {
    return this.api.getPheonix('/v1/VideoPlayer', { 'featureName' :'Ingrain', 'container': 'mp4', 'appserviceuid' : '00040560-0000-0000-0000-000000000000' })
  }

}
