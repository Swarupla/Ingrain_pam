import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-confirmation-popup',
  templateUrl: './confirmation-popup.component.html',
  styleUrls: ['./confirmation-popup.component.scss']
})
export class ConfirmationPopUpComponent implements OnInit {

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig,
     private _notificationService: NotificationService) { }

  submitFlag: boolean;
  ngOnInit() {
  }

  onClose() {
    this._dialogRef.close();
  }

  onConfirm() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
   this._notificationService.success('Your Data will be downloaded shortly');
  }
}
