import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-view-datasetrecords',
  templateUrl: './view-datasetrecords.component.html',
  styleUrls: ['./view-datasetrecords.component.scss']
})
export class ViewDatasetrecordsComponent implements OnInit {

  datasettable;
  columnslist;
  page = 1;
  constructor(private dr: DialogRef, private dc: DialogConfig) { }

  ngOnInit() {
    this.datasettable = this.dc.data['Datasettable'];
    this.columnslist = this.dc.data['Datasetcolumnlist'];
  }

  onClose() {
    this.dr.close();
  }

}
