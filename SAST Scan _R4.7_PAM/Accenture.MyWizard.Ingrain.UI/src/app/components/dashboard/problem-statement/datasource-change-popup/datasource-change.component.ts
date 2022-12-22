import { Component, OnInit } from '@angular/core';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-datasource-change',
  templateUrl: './datasource-change.component.html',
  styleUrls: ['./datasource-change.component.scss']
})
export class DatasourceChangeComponent implements OnInit {

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig, private _notificationService: NotificationService) { }
  submitFlag: boolean;
  connectFlag: boolean;

  ngOnInit() {
  }

  onClose() {
    this._dialogRef.close();
  }

  onConfirm() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
  }
}
