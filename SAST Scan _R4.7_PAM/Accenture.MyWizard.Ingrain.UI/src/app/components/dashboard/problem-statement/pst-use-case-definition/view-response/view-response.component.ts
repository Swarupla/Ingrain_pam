import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-view-response',
  templateUrl: './view-response.component.html',
  styleUrls: ['./view-response.component.scss']
})

export class ViewResponseComponent implements OnInit {
  @Output() outputData = new EventEmitter<any>();
  @Input() title : string;
  @Input() apiResponseEnabled : boolean;
  @Input() correlationId : string;
  @Input() previousQuery : string;

  //Custom Query declaration part starts.
  queryResponseEnabled: boolean = false;
  //Custom Query declaration part ends.

  //Custom API declaration part starts.
  apiurl: string;
  dynamicArray: any;
  bodyparameter: string;
  tokenurl: string;
  isSensitiveData: boolean;
  isAuthenticationtoken: boolean;
  authenticationtoken: string;
  granttype: string;
  tokentext: boolean;
  credentialdetail: boolean;
  resource: string;
  clientsecret: string;
  clientid: string;
  MethodType: string;
  Type: string;
  fetchType : string;
  UseIngrainAzureCredentials : boolean;
  //Custom Query declaration part ends.

  constructor(public modalRef: BsModalRef, 
    private appUtilsService: AppUtilsService, private _notificationService: NotificationService,
    private apiService: ApiService) { }

  ngOnInit(): void {
    this.dynamicArray = [];
    this.title = this.title ? this.title  : 'View Response';
    this.queryResponseEnabled = this.queryResponseEnabled ? this.queryResponseEnabled : false;
    this.apiResponseEnabled = this.apiResponseEnabled ? this.apiResponseEnabled : false;
    this.correlationId = this.correlationId ? this.correlationId : null;
    if (this.apiResponseEnabled) {
      this.getApiResponse();
    }

  }

  getApiResponse() {
    this.appUtilsService.loadingStarted();
    this.apiService.get('GetCustomSourceDetails', { 'correlationid': this.correlationId, 'CustomSourceType': CustomDataTypes.API }).subscribe(response => {
      this.appUtilsService.loadingEnded();
      this.bindApiFormData(response);
    }, error => {
      this.appUtilsService.loadingEnded();
      this._notificationService.error('something went wrong while fetching API Response');
    });
  }
  
  bindApiFormData(response) {
    this.apiurl = response.Data.ApiUrl;
    this.MethodType = response.Data.MethodType;
    this.Type = response.Data.Type;
    this.dynamicArray = this.getKeyValues(response.Data.KeyValues);
    this.bodyparameter = JSON.stringify(response.Data.BodyParam);
    this.isSensitiveData = response.DbEncryption;
    this.isAuthenticationtoken = (response.Data.Authentication.Type == "Token") ? true : false;
    this.authenticationtoken = response.Data.Authentication.Token ? response.Data.Authentication.Token : '';
    this.tokenurl = response.Data.Authentication.AzureUrl ? response.Data.Authentication.AzureUrl : '';
    this.tokentext = this.isAuthenticationtoken; // this i need to check
    this.credentialdetail = (response.Data.Authentication.Type == "AzureAD") ? true : false;
    this.granttype = response.Data.Authentication.AzureCredentials.grant_type ? response.Data.Authentication.AzureCredentials.grant_type : '';
    this.clientid = response.Data.Authentication.AzureCredentials.client_id ? response.Data.Authentication.AzureCredentials.client_id : '';
    this.clientsecret = response.Data.Authentication.AzureCredentials.client_secret ? response.Data.Authentication.AzureCredentials.client_secret : '';
    this.resource = response.Data.Authentication.AzureCredentials.resource ? response.Data.Authentication.AzureCredentials.resource : '';
    this.fetchType = response.Data.fetchType;
    this.UseIngrainAzureCredentials = response.Data.Authentication.UseIngrainAzureCredentials;
  }

  getKeyValues(jsonObject) {
    let dynamicArray = [];
    for (var i in jsonObject){
      dynamicArray.push({key : i, value : jsonObject[i]});
    }
    return dynamicArray;
  }

  saveQuery(data){
    this.outputData.emit(data);
    this.modalRef.hide();
  }

  onClose() {
    this.modalRef.hide();
  }
}
