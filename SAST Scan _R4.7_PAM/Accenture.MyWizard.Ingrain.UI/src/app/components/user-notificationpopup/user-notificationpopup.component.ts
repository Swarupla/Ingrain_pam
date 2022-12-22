import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-user-notificationpopup',
  templateUrl: './user-notificationpopup.component.html',
  styleUrls: ['./user-notificationpopup.component.scss']
})
export class UserNotificationpopupComponent implements OnInit {

  confirmationFlag: boolean;
  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig) { }

  ngOnInit() {
  }

  onConfirm() {
    this.confirmationFlag = true;
    this._dialogRef.close(this.confirmationFlag);
  }

}

