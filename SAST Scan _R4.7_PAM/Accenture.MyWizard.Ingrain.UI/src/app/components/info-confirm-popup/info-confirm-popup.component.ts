import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';

@Component({
  selector: 'app-info-confirm-popup',
  templateUrl: './info-confirm-popup.component.html',
  styleUrls: ['./info-confirm-popup.component.scss']
})
export class InfoConfirmPopupComponent implements OnInit {

  constructor(private _dialogRef: DialogRef) { }

  submitFlag = false;
  ngOnInit() {
  }

  onCancel() {
    this.submitFlag = false;
    this._dialogRef.close();
  }

  onConfirm() {
    this.submitFlag = true;
    this._dialogRef.close(this.submitFlag);
  }

  onClose() {
    this.submitFlag = false;
    this._dialogRef.close();
  }

}
