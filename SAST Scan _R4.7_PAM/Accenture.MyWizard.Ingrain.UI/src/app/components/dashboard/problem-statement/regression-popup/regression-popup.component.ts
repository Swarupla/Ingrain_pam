import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';

@Component({
  selector: 'app-regression-popup',
  templateUrl: './regression-popup.component.html',
  styleUrls: ['./regression-popup.component.scss']
})
export class RegressionPopupComponent implements OnInit {

  constructor(private _dialogRef: DialogRef) { }
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
}
