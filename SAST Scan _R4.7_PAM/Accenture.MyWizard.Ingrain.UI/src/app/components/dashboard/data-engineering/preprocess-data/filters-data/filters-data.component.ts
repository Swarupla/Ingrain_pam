import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { ShowDataComponent } from '../show-data/show-data.component';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { ConfirmationPopUpComponent } from '../confirmation-pop-up/confirmation-popup.component';
import { ExcelService } from 'src/app/_services/excel.service';
import { ViewDataComponent } from '../view-data/view-data.component';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-filters-data',
  templateUrl: './filters-data.component.html',
  styleUrls: ['./filters-data.component.scss']
})
export class FiltersDataComponent implements OnInit, OnChanges {

  @Input() filters;
  @Input() showDataIcon;
  @Input() problemTypeFlag;
  @Input() isLoading;
  @Input() correlationId;
  @Input() modelType;

  @Output() setDatatoserverFilterValuesOnChange = new EventEmitter();
  @Output() setDatatoserverFilterValue = new EventEmitter();
  @Output() setDefaultFilterOptions = new EventEmitter();

  tableData = [];
  showDataColumnsList = [];
  tableDataOfTimeSeries = {};
  defFilterOptions = {};
  defaultFilterOptions = {};
  defaultFilterOptionsMain = [];
  filterOptions = {};
  filterAttributes = [];
  selectedFilterAttributes: [] = [];
  tableDataforDownload;
  userID;
  @Input() readOnly;
  dataToServer = {
    'Filters': {}
  };
  decimalPoint;

  constructor(private des: DataEngineeringService, private excelService: ExcelService, private appUtilsService: AppUtilsService,
    private dialogService: DialogService, private ns: NotificationService) {
      this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
     }

  ngOnInit() {
    this.initFilterData();
  }

  ngOnChanges() {
    this.initFilterData();
  }

  showData() {
    // Bug 712725 Ingrain_StageTest_R2.1 - Sprint 2 - Unable to close the data quality screen by clicking on close button
    // Fix : Added loader
    this.appUtilsService.loadingStarted();
    this.des.getShowData(this.correlationId, 100, false, this.modelType, this.decimalPoint).subscribe(da => {
      //  var da = this.x;
      this.tableData = da['InputData'] || [];
      this.tableDataOfTimeSeries = da['TimeSeriesInputData'] || [];
      this.showDataColumnsList = da['ColumnList'].replace('[', '').slice(0, -1).split(',').map(s => s.trimStart()) || [];
      this.appUtilsService.loadingEnded();
      this.dialogService.open(ShowDataComponent, {
        data: {
          'tableData': this.tableData,
          'tableDataOfTimeSeries': this.tableDataOfTimeSeries,
          'columnList': this.showDataColumnsList,
          'problemTypeFlag': this.problemTypeFlag
        }
      });
    }
    );
  }

  downloadData() {
    this.dialogService.open(ConfirmationPopUpComponent, {}).afterClosed.subscribe(poupdata => {
      if (poupdata === true) {
        this.des.getShowData(this.correlationId, 0, true, this.modelType, this.decimalPoint).subscribe(da => {
          if (this.problemTypeFlag) {
            this.tableDataforDownload = da['TimeSeriesInputData'] || [];
          } else {
            this.tableDataforDownload = da['InputData'] || [];
          }
          const self = this;
          if ((sessionStorage.getItem('Environment') === 'PAM' || sessionStorage.getItem('Environment') === 'FDS')) {
            self.excelService.exportAsExcelFile(self.tableDataforDownload, 'DownloadedData');
          } else {
            self.excelService.exportAsPasswordProtectedExcelFile(self.tableDataforDownload, 'DownloadedData').subscribe(response => {
              self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
              let binaryData = [];
              binaryData.push(response);
              let downloadLink = document.createElement('a');
              downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
              downloadLink.setAttribute('download', 'DownloadedData' + '.zip');
              document.body.appendChild(downloadLink);
              downloadLink.click();
            }, (error) => {
              console.log(error);
            });
          }
          
        });
      }
    });
  }

  viewData() {
    this.userID = this.appUtilsService.getCookies().UserId;
    const viewDataOnclose = this.dialogService.open(ViewDataComponent, {
      data: {
        'correlationId': this.correlationId,
        'userId': this.userID,
      }
    }).afterClosed.subscribe();

  }

  // onChange(tabName, attribute, selectedOption, inputValue) {
  onChange(tabName, selectedOptions, inputValue) {
    const attribute = selectedOptions['parent'];
    const selectedOption = selectedOptions['child'];

    if (selectedOption === 'CustomValue') {
      if (inputValue !== null && inputValue !== undefined) {
        const params = [tabName, attribute, selectedOption, inputValue];
        this.setDatatoserverFilterValuesOnChange.emit(params);
      }
    } else if (selectedOptions['child'].length === 0) {
      const params = ['Filters', selectedOptions['parent'], 'null', null];
      this.setDatatoserverFilterValuesOnChange.emit(params);
    } else {
      const params = [tabName, attribute, selectedOption, null];
      this.setDatatoserverFilterValuesOnChange.emit(params);
    }
  }

  initFilterData() {

    for (const i in this.filters) {
      if (this.filters.hasOwnProperty(i)) {
        this.filterAttributes.push(i);
        for (const j in this.filters[i]) {

          if (this.filters[i].hasOwnProperty(j)) {
            if (this.filters[i][j] === '') {
              delete this.filters[i][j];
            }
            if (j === 'ChangeRequest' || j === 'PChangeRequest') {
              delete this.filters[i][j];
            }
          }

        }
        this.filterOptions[i] = Object.keys(this.filters[i]);
        const x = this.getFilteredDataByValue(this.filters[i], 'True');

        // x === undefined || x.length === 0 ? this.defaultFilterOptions[i] = [] : this.defaultFilterOptions[i] = [x][0];
        x === undefined || x.length === 0 ? this.defaultFilterOptions[i] = [] : this.defaultFilterOptions[i] = x;
        if (this.defaultFilterOptions[i].length > 0) {
          this.defaultFilterOptionsMain.push(i);
          // BugFix for 995519
          this.dataToServer.Filters[i] = [];
          for (const j in this.defaultFilterOptions[i]) {
            const tempObj = {};
            tempObj[this.defaultFilterOptions[i][j]] = 'True';
            this.dataToServer.Filters[i].push(tempObj);
          }
        }
      }

    }
    this.SetFilterAttributes(this.defaultFilterOptionsMain);
    this.setDefaultFilterOptions.emit(this.defaultFilterOptions);
    this.defFilterOptions = Object.assign({}, this.defaultFilterOptions);
  }


  // Will return a array by filtering 'object' with 'value'.
  getFilteredDataByValue(object: any, value: string) {
    const copyObject = object;
    if (!copyObject) {
      return '';
    }

    delete copyObject.ChangeRequest;
    delete copyObject.PChangeRequest;
    const result = Object.keys(copyObject).filter(key => copyObject[key] === value);
    return result;
  }

  SetFilterAttributes(selectedValues) {
    this.selectedFilterAttributes = selectedValues;
    if (selectedValues.length > 0) {
      selectedValues.forEach((element, index) => {
        if (this.dataToServer["Filters"][element] == undefined)
          this.dataToServer["Filters"][element] = [];
      });
    }
    this.setDatatoserverFilterValue.emit(this.dataToServer.Filters);
  }

  removeAttribute(selectedValues) {
    this.selectedFilterAttributes = selectedValues;
    this.defFilterOptions[selectedValues] = [];
    const params = ['Filters', selectedValues, 'null', null];
    this.setDatatoserverFilterValuesOnChange.emit(params);
  }

  removeDataAttribute(selectedValues) {
    for (const i in this.dataToServer["Filters"][selectedValues.parent]) {
      if (this.dataToServer["Filters"][selectedValues.parent][i].hasOwnProperty(selectedValues.child))
        this.dataToServer["Filters"][selectedValues.parent][i] = "False";
    }
    this.setDatatoserverFilterValue.emit(this.dataToServer.Filters);
  }

}
