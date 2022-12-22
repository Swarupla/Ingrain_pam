import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-cookie-decline-popup',
  templateUrl: './cookie-decline-popup.component.html',
  styleUrls: ['./cookie-decline-popup.component.scss']
})
export class CookieDeclinePopupComponent implements OnInit {

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
  }

  onCancel() {
    this.submitFlag = false;
    this._dialogRef.close(this.submitFlag);
  }

}
