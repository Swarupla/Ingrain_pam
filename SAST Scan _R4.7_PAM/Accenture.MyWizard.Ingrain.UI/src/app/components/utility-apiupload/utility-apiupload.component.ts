import { Component, OnInit, ElementRef, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { Validators, FormBuilder, FormGroup } from '@angular/forms';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { ProblemStatementService } from '../../_services/problem-statement.service';
import { CoreUtilsService } from '../../_services/core-utils.service';
import { throwError, of, timer, EMPTY } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { Tablegrid } from 'src/app/components/dashboard/problem-statement/tablegrid';

@Component({
  selector: 'app-utility-apiupload',
  templateUrl: './utility-apiupload.component.html',
  styleUrls: ['./utility-apiupload.component.scss']
})
export class UtilityApiuploadComponent implements OnInit {

  params;
  tokentext = true;
  credentialdetail = false;
  methodtype = [{ 'name': 'POST' }];
  startDate = new Date();
  methodType;
  datasetName;
  apiurl;
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
  deliveryTypeName;
  clientUId;
  userid;
  startdate;
  enddate;
  specialcharflagurl: boolean;
  errorvalid: boolean;
  jsonvalid: boolean;
  tokenerrorvalid: boolean;
  isPrivate: boolean = true;
  PIConfirmation;

  dynamicArray: Array<Tablegrid> = [];
  newDynamic: any = {};

  constructor(private ns: NotificationService, private formBuilder: FormBuilder,
    private coreUtilsService: CoreUtilsService,
    public dialog: DialogRef, private dialogConfig: DialogConfig, private problemStatementService: ProblemStatementService,
    private toastr: ToastrService) { }

  ngOnInit() {
    // const userId = sessionStorage.getItem('userId');
    this.errorvalid = false;
    this.tokenerrorvalid = false;
    this.specialcharflagurl = false;
    if (this.dialogConfig.hasOwnProperty('data')) {
      this.deliveryTypeName = this.dialogConfig.data.deliveryConstructUID;
      this.clientUId = this.dialogConfig.data.clientUID;
      this.userid = this.dialogConfig.data.userId;
    }
    this.apiRow = this.formBuilder.group({
      //  apiSelect: [''],
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
  }

  showcredentialdetail() {
    this.credentialdetail = true;
    this.tokentext = false;
    this.authType = 'AzureAD';
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

  onClose() {
    this.dialog.close();
  }

  onPIConfirmation(optionValue) {
    this.PIConfirmation = optionValue;
    if (this.PIConfirmation) {
      this.ns.success('Personal Identification Information is available in the data provided for analysis. Data will be encrypted and stored by the platform.')
    }
  }

  // onSwitchChanged(elementRef) {
  //   if (elementRef.checked === true) {
  //      this.isPublic = true;       
  //   }
  //   if (elementRef.checked === false) {
  //     this.isPublic= false;     
  //   }
  // }

  onSubmit() {
    if (this.coreUtilsService.isNil(this.datasetName)) {
      this.ns.error('Please Enter the DataSet Name');
    }
    else if (this.errorvalid === true || this.coreUtilsService.isNil(this.apiurl)) {
      this.ns.error('Please Enter the Valid API URL');
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
            } else if (this.credentialdetail === true && (this.coreUtilsService.isNil(this.tokenurl) ||
              this.coreUtilsService.isNil(this.granttype) || this.coreUtilsService.isNil(this.clientid) ||
              this.coreUtilsService.isNil(this.clientsecret) || this.coreUtilsService.isNil(this.resource))) {
              this.ns.error('Please Enter All the Credential Details for Generating the Token.');
            } else if (this.tokenerrorvalid === true && !(this.coreUtilsService.isNil(this.tokenurl))) {
              this.ns.error('Please Enter the Valid Token URL');
            }
            else if (this.isIncremental === true && (this.coreUtilsService.isNil(this.apiRow.get('startDateControl').value) || this.coreUtilsService.isNil(this.apiRow.get('endDateControl').value))) {
              this.ns.error('Please Enter Start and End Date');
            } else {
              let startDate = '';
              let enddate = '';
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
                'DeliveryConstructUId': this.deliveryTypeName,
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
                'startdate': startDate,
                'enddate': enddate,
                'userid': this.userid,
                'Encryption': this.PIConfirmation
              };
              const finalPayload = [];
              finalPayload.push(this.params);
              this.dialog.close(finalPayload);
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
}
