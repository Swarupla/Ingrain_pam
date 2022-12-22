import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-add-filter',
  templateUrl: './add-filter.component.html',
  styleUrls: ['./add-filter.component.scss']
})
export class AddFilterComponent implements OnInit {
  filterData: [] = [];
  filtersTobeRemoved = new Set();

  constructor(private dr: DialogRef, private dc: DialogConfig) { }

  ngOnInit() {
    this.filterData = this.dc.data;
  }

  onClick(value) {
    this.filtersTobeRemoved.add(value);
  }

  onClose() {
    this.dr.close(Array.from(this.filtersTobeRemoved));
  }

}
