import { Component, OnInit } from '@angular/core';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-valid-record-details-popup',
  templateUrl: './valid-record-details-popup.component.html',
  styleUrls: ['./valid-record-details-popup.component.scss']
})
export class ValidRecordDetailsPopupComponent implements OnInit {

  submitFlag: boolean;
  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig,
    private _notificationService: NotificationService) { }

  validRecordDetails;
  message: string;
  ifLessRecords: boolean = false;

  ngOnInit() {   
    this.validRecordDetails = this._dialogConfig.data.Message;
    this.submitFlag = false;
    if (this.validRecordDetails.Records[0] >= 4 && this.validRecordDetails.Records[0] < 20) {
      this.message = `The accuracy of the model will be impacted as less than 20 records are available for training. 
      Do you wish to proceed?`;
      this.ifLessRecords = true;
    } else if (this.validRecordDetails.Records[0] >= 20) {
      this.message = this.validRecordDetails['Msg'];
    }
    // this.message = this._dialogConfig.data.Message;
  }

  onClose() {
    // this.submitFlag = false;
    this._dialogRef.close();
  }

  onConfirm() {
    this.submitFlag = false;
    this._dialogRef.close(this.submitFlag);
  }

  redirectToDashboard() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
  }

}
