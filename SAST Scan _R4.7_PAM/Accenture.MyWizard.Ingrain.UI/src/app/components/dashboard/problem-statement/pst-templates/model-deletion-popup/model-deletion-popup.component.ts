import { Component, OnInit } from '@angular/core';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';


@Component({
  selector: 'app-model-deletion-popup',
  templateUrl: './model-deletion-popup.component.html',
  styleUrls: ['./model-deletion-popup.component.scss']
})
export class ModelDeletionPopupComponent implements OnInit {

  modelName: string;
  cascadeModel: boolean;
  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig, private _notificationService: NotificationService) { }
  isDeleteFlag: boolean;
  ngOnInit() {
    this.modelName = this._dialogConfig.data.modelName;
    this.cascadeModel = this._dialogConfig.data.isCascadeModel; 
  }

  onClose() {
    this._dialogRef.close();
  }

  onConfirm() {
    this.isDeleteFlag = true;
    this._dialogRef.close(this.isDeleteFlag);
  }
}
