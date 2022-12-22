import { Component, OnInit, ElementRef, Output, EventEmitter, Input } from '@angular/core';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { throwError, Subscription } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';

@Component({
  selector: 'app-add-newapp',
  templateUrl: './add-newapp.component.html',
  styleUrls: ['./add-newapp.component.scss'],
  host: { '(document:click)': 'onClick($event)' }
})
export class AddNewappComponent implements OnInit {

  @Input() Flag;
  @Output() saved = new EventEmitter();
  @Output() closed = new EventEmitter();
  constructor(private _eref: ElementRef, private _ProblemStatementService: ProblemStatementService,
    private _NotificationService: NotificationService, private _appUtilsService: AppUtilsService,
    private _localStorageService: LocalStorageService) {
      this.show = true;
    }
  show: boolean;
  appname: '';
  correlationId;
  baseurl: '';
  error: boolean;
  flag: boolean;
  userID: string;
  value: string;
  errorvalid: boolean;
  errorspecialchar: boolean;
  btndisable: boolean;
  subscription: Subscription;
  clientUId: string;
  deliveryConstructUID: string;
  isdisable: boolean;
  appval: string;
  key;
  specialcharflag: boolean;
  specialcharflagurl: boolean;

  ngOnInit() {
    this.show = true;
    this.correlationId = this._localStorageService.getCorrelationId();
    this.userID = this._appUtilsService.getCookies().UserId;
    this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {
      this.clientUId = paramData.clientUID;
      this.deliveryConstructUID = paramData.deliveryConstructUId;
    });
   // this.AppEditable();
  }


  AppEditable() {
    this._ProblemStatementService.appeditable(this.correlationId).subscribe(data => {
      this.isdisable = data;
      // console.log(this.appsData);
    });
  }

  onClose() {
    this.closed.emit('close');
    this.show = false;
  }

  onClick(event) {
    console.log(this.Flag);
   if (this.Flag === true) {
    if (event.target.id !== 'Add'
      && event.target.id !== 'close' && this.Flag === true) {
     // if (this.isdisable) {
        this.show = true;
      // } else { this.show = false; }
      // this.error = false;
      // this.flag = false;
      // this.errorvalid = false;
      // this.errorspecialchar = false;
    } else if (event.target.id === 'close' || (event.target.id === 'Add' && this.error !== true)) {
      this.show = false;
      this.appname = '';
      this.baseurl = '';
    } } else {
      this.show = false;
    }

  }

  validatespecialchar(val: string) {
    this.specialcharflag = this.checkspecialchar(val);
    if (!this.specialcharflag) {
      this.errorspecialchar = false;
      if (this.baseurl !== undefined) {
        this.specialcharflagurl = this.checkspecialcharurl(this.baseurl);
        if (this.specialcharflagurl) {
          this.btndisable = true;
        } else {
          this.btndisable = false;
        }
      }
    } else {
      this.errorspecialchar = true;
      this.btndisable = true;
    }
  }

  onKey(val: string) {
    if (val === undefined) {
      val = '';
    }
    this.appval = this.appname;
    if (val !== '') {
      if ((val.startsWith('https://') && val.length > 8 && val.includes('.')) ||
        (val.startsWith('http://') && val.length > 7 && val.includes('.'))) {
        this.specialcharflagurl = this.checkspecialcharurl(val);
        if (this.specialcharflagurl) {
          this.errorvalid = true;
          this.btndisable = true;
        } else {
          this.errorvalid = false;
          if (this.appval.length > 0) {
            this.specialcharflag = this.checkspecialchar(this.appval);
            if (!this.specialcharflag) {
              this.btndisable = false;
            } else {
              this.btndisable = true;
            }
          }
          // this.btndisable = false;
        }
      } else {
        this.errorvalid = true;
        this.btndisable = true;
      }
    }
  }

  checkspecialchar(val: string) {
    const regex = /^[A-Za-z]*$/;
    const isValid = regex.test(val);
    if (!isValid) {
      return true;
    } else {
      return false;
    }
  }

  checkspecialcharurl(val: string) {
    const reg = /^[?a-zA-Z0-9://.&_-]+$/;
    const regvalid = reg.test(val);
    if (!regvalid) {
      return true;
    } else {
      return false;
    }
  }

  Savenewapp(appname, baseurl) {
    this.appname = appname.ApplicationName;
    this.baseurl = baseurl.BaseURL;
    if (appname.valid && baseurl.valid) {
      this.error = false;
      const data = {
        'ApplicationName': appname.model,
        'BaseURL': baseurl.model,
        'CreatedByUser': this.userID,
        'clientUId': this.clientUId,
        'deliveryConstructUID': this.deliveryConstructUID
      };
      this._appUtilsService.loadingStarted();
      this._ProblemStatementService.saveNewApp(data).subscribe(result => {

        if (result === 'Success') {
          this._NotificationService.success('Application successfully added');
          this.closed.emit('Success');
          this.saved.emit('Success');
          this._appUtilsService.loadingEnded();
        } else if (result === 'Error') {
          this.closed.emit('Error');
          this._appUtilsService.loadingEnded();
          this._NotificationService.success('Application is not added');
        }
      }, (error) => {
        this._appUtilsService.loadingEnded();
        if (error.error === 'Application name already present') {
          // this.flag = true;
          this.closed.emit('Error');
          this._NotificationService.error('Application name already present');
        } else {
          this.closed.emit('Error');
          this._NotificationService.error('Some thing went wrong.');
        }
      });
    } else {
      this.error = true;
    }
  }


}
