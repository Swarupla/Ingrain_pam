import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { BsModalService } from 'ngx-bootstrap/modal';
import { Tablegrid } from 'src/app/components/dashboard/problem-statement/tablegrid';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { TargetNodeComponent } from '../target-node/target-node.component';

enum FetchType {
  single = "single",
  multiple = "multiple"
}
@Component({
  selector: 'app-custom-data-api',
  templateUrl: './custom-data-api.component.html',
  styleUrls: ['./custom-data-api.component.scss']
})
export class CustomDataApiComponent implements OnInit {
  @Output() APIData = new EventEmitter<any>();
  @Input() ApiConfig: any;

  isDataSetNameRequired: boolean;
  isIncrementalRequired: boolean;
  isCategoryRequired: boolean;
  isFetchTypeRequired: boolean;
  params;
  tokentext = true;
  credentialdetail = false;
  methodtype = [{ 'name': 'POST' }];
  startDate = new Date();
  methodType;
  datasetName;
  apiurl : string;
  headerkey;
  headervalue;
  bodyparameter;
  authenticationtoken;
  granttype;
  clientid;
  clientsecret;
  resource;
  tokenurl;
  apiRow: FormGroup;
  isIncremental = false;
  isAuthenticationtoken = true;
  isCredential = true;
  authType = 'Token';
  deliveryConstructUId;
  clientUId;
  userid;
  startdate;
  enddate;
  specialcharflagurl: boolean;
  errorvalid: boolean;
  jsonvalid: boolean;
  tokenerrorvalid: boolean;
  isPrivate: boolean = true;
  PIConfirmation: boolean;
  fetchType = FetchType.multiple;
  UseIngrainAzureCredentials: boolean = false;
  paramData: any;

  dynamicArray: Array<Tablegrid> = [];
  newDynamic: any = {};
  finalPayload : any = [];
  targetNodes = [];
  selectedTargetNode : string;
  saveDisabled : boolean = true;
  jsonTargetNode : string;
  invalidDomain : boolean = false;
  environmentDomain : string;

  constructor(private ns: NotificationService, private formBuilder: FormBuilder,
    private coreUtilsService: CoreUtilsService, private apputilService: AppUtilsService, private apiService : ApiService, private _bsModalService : BsModalService,
    private envService: EnvironmentService) { }

  ngOnInit() {
    const domainName = this.envService.environment.ingrainAPIURL;
    this.environmentDomain = domainName.substring(0,domainName.indexOf('.com') + 4);
    // Api configuration initialization starts.
    this.isDataSetNameRequired = (this.ApiConfig?.isDataSetNameRequired !== undefined) ? this.ApiConfig.isDataSetNameRequired : true;
    this.isCategoryRequired = (this.ApiConfig?.isCategoryRequired !== undefined) ? this.ApiConfig.isCategoryRequired : true;
    this.isIncrementalRequired = (this.ApiConfig?.isIncrementalRequired !== undefined) ? this.ApiConfig.isIncrementalRequired : true;
    this.isFetchTypeRequired = (this.ApiConfig?.isFetchTypeRequired !== undefined) ? this.ApiConfig.isFetchTypeRequired : true;
    // Api configuration initialization ends.

    this.errorvalid = false;
    this.tokenerrorvalid = false;
    this.specialcharflagurl = false;
    this.apputilService.getParamData().subscribe(paramData => {
      this.paramData = paramData;
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUId = paramData.deliveryConstructUId;
    });
    this.userid = this.apputilService.getCookies().UserId;

    this.apiRow = this.formBuilder.group({
      startDateControl: [''],
      endDateControl: ['']
    });

    this.newDynamic = { key: "", value: "" };
    this.dynamicArray.push(this.newDynamic);
  }

  showtokentext() {
    this.tokentext = true;
    this.credentialdetail = false;
    this.authType = 'Token';
    this.UseIngrainAzureCredentials = false;
    this.resetCredetials();
    this.checkApiUrlMatchesDomain();
  }

  showcredentialdetail() {
    this.credentialdetail = true;
    this.tokentext = false;
    this.authType = 'AzureAD';
    this.UseIngrainAzureCredentials = true;
    this.authenticationtoken = undefined;//remove token added incase of credentials are choosen.
    this.checkApiUrlMatchesDomain();
  }

  selectedMethod(event) {
    this.methodType = event.target.value;
  }

  onCheckedIncrementalData(elementRef) {
    if (elementRef.checked === true) {
      this.isIncremental = true;
      this.isAuthenticationtoken = false;
      this.authType = 'AzureAD';
      this.credentialdetail = true;
      this.tokentext = false;
      this.authenticationtoken = '';
    } else {
      this.isIncremental = false;
      this.isAuthenticationtoken = true;
      this.authType = 'Token';
      this.tokentext = true;
      this.credentialdetail = false;
    }
  }

  onPIConfirmation(optionValue) {
    this.PIConfirmation = optionValue;
    if (this.PIConfirmation) {
      this.ns.success('Personal Identification Information is available in the data provided for analysis. Data will be encrypted and stored by the platform.')
    }
  }

  setIngrainToken(value) {
    this.UseIngrainAzureCredentials = value;
    if(this.UseIngrainAzureCredentials && this.credentialdetail)//discard credetials added incase of Token are choosen.
    {
      this.resetCredetials();
    }
    this.checkApiUrlMatchesDomain();
  }
  resetCredetials(){
    this.granttype = undefined;
      this.clientid = undefined;
      this.clientsecret = undefined;
      this.resource = undefined;
      this.tokenurl = undefined;
  }

  testAPI() {
    this.saveDisabled = true;
    this.finalPayload = [];
    if (this.isDataSetNameRequired && this.coreUtilsService.isNil(this.datasetName)) {
      this.ns.error('Please Enter the DataSet Name');
    }
    else if (this.errorvalid === true || this.coreUtilsService.isNil(this.apiurl)) {
      this.ns.error('Please Enter the Valid API URL');
    }else if(!(this.apiurl.includes(this.environmentDomain)) && this.UseIngrainAzureCredentials){
      this.ns.error('For Authentication Method – ‘Same as Ingrain’, API URL entered is Incorrect. URL Host should be of current Environment that is - ' +this.environmentDomain);
    }
    else {
      let keyvalueflag = false;
      let headers = {};
      if (this.dynamicArray.length > 0) {
        this.dynamicArray.forEach(element => {
          if (element['key'] == '' && element['value'] == '') {
            headers = {};
          } else if (element['key'] == '' || element['value'] == '') {
            keyvalueflag = true;
          } else {
            const headerkey = element['key'];
            const headervalue = element['value'];
            headers[headerkey] = headervalue;
          }
        });
      }
      if (keyvalueflag !== false) {
        this.ns.error("Key & Value can not be Blank");
      } else {
        if (!this.coreUtilsService.isNil(this.bodyparameter)) {
          this.jsonvalid = this.isJson(this.bodyparameter);
          if (this.jsonvalid === true) {
            const isdata = JSON.stringify(JSON.parse(this.bodyparameter));
            if (isdata === '{}') {
              this.ns.error('Enter the Correct JSON Object for Body');
            } else if (((this.tokentext === true || this.isAuthenticationtoken === true) && this.credentialdetail === false) &&
              this.coreUtilsService.isNil(this.authenticationtoken)) {
              this.ns.error('Please Enter the Token for Authentication');
            } else if ((this.credentialdetail === true && !this.UseIngrainAzureCredentials) && (this.coreUtilsService.isNil(this.tokenurl) ||
              this.coreUtilsService.isNil(this.granttype) || this.coreUtilsService.isNil(this.clientid) ||
              this.coreUtilsService.isNil(this.clientsecret) || this.coreUtilsService.isNil(this.resource))) {
              this.ns.error('Please Enter All the Credential Details for Generating the Token.');

            } else if (this.tokenerrorvalid === true && !(this.coreUtilsService.isNil(this.tokenurl))) {
              this.ns.error('Please Enter the Valid Token URL');
            }
            else if (this.isIncremental === true && (this.coreUtilsService.isNil(this.apiRow.get('startDateControl').value) || this.coreUtilsService.isNil(this.apiRow.get('endDateControl').value))) {
              this.ns.error('Please Enter Start and End Date');
            } else {
              let startDate, enddate;
              if (!this.coreUtilsService.isNil(this.apiRow.get('startDateControl').value) && !this.coreUtilsService.isNil(this.apiRow.get('endDateControl').value)) {
                startDate = this.apiRow.get('startDateControl').value.toLocaleDateString();
                enddate = this.apiRow.get('endDateControl').value.toLocaleDateString();
              }

              if (this.PIConfirmation === undefined) {
                return this.ns.error('Kindly select the PII data confirmation options.');
              }

              this.params = {
                'payloadfor': 'API',
                'HttpMethod': 'POST',
                'ClientUId': this.clientUId,
                'DeliveryConstructUId': this.deliveryConstructUId,
                'DatasetName': this.datasetName,
                'IsPrivate': this.isPrivate,
                'Url': this.apiurl,
                'Keyvalue': headers,
                'Category': 'null',
                'EnableIncrementalFetch': this.isIncremental,
                'Body': JSON.parse(this.bodyparameter),
                'authttype': this.authType,
                'token': this.coreUtilsService.isNil(this.authenticationtoken) ? '' : this.authenticationtoken,
                'tokenurl': this.coreUtilsService.isNil(this.tokenurl) ? '' : this.tokenurl,
                'clientid': this.clientid,
                'clientsecret': this.clientsecret,
                'resource': this.resource,
                'grantype': this.granttype,
                'source': CustomDataTypes.API,
                'startdate': startDate,
                'enddate': enddate,
                'userid': this.userid,
                'PIConfirmation': this.PIConfirmation,
                'fetchType': this.fetchType,
                'UseIngrainAzureCredentials': this.UseIngrainAzureCredentials
              };
               
              this.finalPayload.push(this.params);
              this.checkApiResponse(this.finalPayload);
            }
          } else {
            this.ns.error('Enter the Correct JSON Object for Body');
          }
        } else {
          this.ns.error('Please Enter the Body Parameter');
        }
      }
    }
  }

  onKey(val: string) {
    if (val === undefined) {
      val = '';
    }
    if (val !== '') {
      if ((val.startsWith('https://') && val.length > 8 && val.includes('.')) ||
        (val.startsWith('http://') && val.length > 7 && val.includes('.'))) {
        this.errorvalid = false;
      } else {
        this.errorvalid = true;
      }
    } else {
      this.errorvalid = false;
    }
    this.checkApiUrlMatchesDomain();
  }


  onKeyazureURL(val: string) {
    if (val === undefined) {
      val = '';
    }
    if (val !== '') {
      if ((val.startsWith('https://') && val.length > 8 && val.includes('.')) ||
        (val.startsWith('http://') && val.length > 7 && val.includes('.'))) {
        this.tokenerrorvalid = false;
      } else {
        this.tokenerrorvalid = true;
      }
    } else {
      this.tokenerrorvalid = false;
    }
  }

  isJson(item) {
    item = typeof item !== "string"
      ? JSON.stringify(item)
      : item;

    try {
      item = JSON.parse(item);
    } catch (e) {
      return false;
    }

    if (typeof item === "object" && item !== null) {
      return true;
    }

    return false;
  }

  checkspecialcharurl(val: string) {
    const reg = /^[?a-zA-Z0-9://.&_-]+$/;
    const regvalid = reg.test(val);
    if (!regvalid) {
      return false;
    } else {
      return true;
    }
  }

  getpublicflag() {
    this.isPrivate = false;
  }

  getprivateflag() {
    this.isPrivate = true;
  }

  addRow(index) {
    this.newDynamic = { key: "", value: "" };
    this.dynamicArray.push(this.newDynamic);
    return true;
  }

  deleteRow(index) {
    if (this.dynamicArray.length == 1) {
      this.ns.warning("Can't delete the row when there is only one row", 'Warning');
      return false;
    } else {
      this.dynamicArray.splice(index, 1);
      return true;
    }
  }

  setFetchType() {
    this.fetchType = (this.fetchType == FetchType.single) ? FetchType.multiple : FetchType.single;
  }

  checkApiResponse(data){
    this.apputilService.loadingStarted();
    var frmData = new FormData();
    var params={
      userId : this.userid,
      clientUID : this.clientUId,
      deliveryUID : this.deliveryConstructUId,
      category : this.paramData.category
    }
        
    frmData.append('CustomDataPull', JSON.stringify(this.getParams(data)));
    this.apiService.post('CheckCustomAPIResponse',frmData,params).subscribe(response=>{
      this.apputilService.loadingEnded();
      const data = (typeof response === 'string') ? JSON.parse(response) : response;
      this.targetNodes = (typeof data.TargetNodes === 'string') ? JSON.parse(data.TargetNodes) : data.TargetNodes;
      this.jsonTargetNode = data.JsonResponse;
      this.getTargetNode();
    },error=>{
      this.apputilService.loadingEnded();
      this.ns.warning("No response is returned from the API. Please check try again!!");
    });
  }

  getParams(apiData){
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
    return {
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
        }
      }
    };
    
  }

  getTargetNode() {
    this._bsModalService.show(TargetNodeComponent, { class: 'modal-dialog modal-dialog-centered', backdrop: 'static', initialState: { title: 'Choose Target Node', NodeList: this.targetNodes, jsonTargetNode : this.jsonTargetNode } }).content.Data.subscribe(selectedNode => {
      if (selectedNode) {
        this.selectedTargetNode = selectedNode;
        this.saveDisabled = false;
      }
    });
  }

  onSubmit() {
    this.finalPayload.push({targetNode : this.selectedTargetNode})//"TargetNode": this.selectedTargetNode
    this.APIData.emit(this.finalPayload);
  }

  checkApiUrlMatchesDomain() {
    this.invalidDomain = false;
    if (!this.coreUtilsService.isNil(this.apiurl)) {
      if (!this.apiurl.includes(this.environmentDomain) && this.UseIngrainAzureCredentials) {
        this.invalidDomain = true;
      }
    }
  }

}
