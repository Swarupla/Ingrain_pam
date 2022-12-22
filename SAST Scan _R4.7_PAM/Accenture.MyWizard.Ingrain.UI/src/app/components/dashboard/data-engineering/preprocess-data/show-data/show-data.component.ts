import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { Router } from '@angular/router';

@Component({
  selector: 'app-show-filter',
  templateUrl: './show-data.component.html',
  styleUrls: ['./show-data.component.scss']
})
export class ShowDataComponent implements OnInit {
  columnHeaders: any[];
  tableData: any;
  page = 1;
  totalRec = 100;
  tabOptions: string[] = [];
  problemTypeFlag = false;
  indexOfSelectedTab = '0';
  activeTab = '';
  tableDataOfTimeSeries: any;

  constructor(private dr: DialogRef, private dc: DialogConfig, public router: Router) { }

  ngOnInit() {
    this.columnHeaders = this.dc.data['columnList'];
    this.problemTypeFlag = this.dc.data['problemTypeFlag'];
    this.tableDataOfTimeSeries = this.dc.data['tableDataOfTimeSeries'];
    this.tableData = this.tableDataOfTimeSeries;
    if (this.problemTypeFlag && this.tableData != undefined) {
      for (const key in this.tableData) {
        if (this.tableData.hasOwnProperty(key)) {
          if (this.tableData[key].length > 0) {
            this.tabOptions.push(key);
          }
        }
      }
      this.activeTab = this.tabOptions[0];
    } else {
      if (this.problemTypeFlag && this.tableData === undefined) {
        this.problemTypeFlag = false;
      }
      this.tableData = this.dc.data['tableData'];
    }
  }

  onTabClick(i, tabName) {
    this.indexOfSelectedTab = i;
    this.activeTab = tabName;
  }

  setMyClasses(index) {

    const flag = (this.indexOfSelectedTab === index);
    return {
      'nav-link': true,
      active: flag,
      show: flag
    };
  }
  onClose() {
    this.dr.close();
  }

}
