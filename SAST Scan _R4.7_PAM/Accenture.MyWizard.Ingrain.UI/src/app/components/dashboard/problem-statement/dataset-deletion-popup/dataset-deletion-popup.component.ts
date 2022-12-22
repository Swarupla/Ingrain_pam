import { Component, OnInit } from '@angular/core';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-dataset-deletion-popup',
  templateUrl: './dataset-deletion-popup.component.html',
  styleUrls: ['./dataset-deletion-popup.component.scss']
})
export class DatasetDeletionPopupComponent implements OnInit {

  DatasetName: string;
  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig, private _notificationService: NotificationService) { }
  isDeleteFlag: boolean;
  ngOnInit() {
    this.DatasetName = this._dialogConfig.data.DatasetName;
  }

  onClose() {
    this._dialogRef.close();
  }

  onConfirm() {
    this.isDeleteFlag = true;
    this._dialogRef.close(this.isDeleteFlag);
  }

}
