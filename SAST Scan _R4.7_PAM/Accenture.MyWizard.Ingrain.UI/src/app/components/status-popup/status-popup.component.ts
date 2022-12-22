import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-status-popup',
  templateUrl: './status-popup.component.html',
  styleUrls: ['./status-popup.component.scss']
})
export class StatusPopupComponent implements OnInit {

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig,
    private _notificationService: NotificationService) { }

  submitFlag: boolean;
  message: string;
  statusMessageText: string;
  ngOnInit() {
    this.message = this._dialogConfig.data.privateModelTrainingMsg;
    this.statusMessageText = this._dialogConfig.data.modelTrainingStatusText;
  }

  onClose() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
  }

}
