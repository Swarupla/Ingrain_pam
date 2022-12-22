import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-custom-column',
  templateUrl: './custom-column.component.html',
  styleUrls: ['./custom-column.component.scss']
})
export class CustomColumnComponent implements OnInit {

  title = 'Enter a custom feature name';
  featureName = '';
  dialoref: any;
  modalNameInput: string;

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig) {
    this.dialoref = _dialogRef;
  }

  ngOnInit() {}

  onClose() {
    this.dialoref.close('closed');
  }

  onEnter(featureName) {
      this.dialoref.close(featureName);
  }

}
