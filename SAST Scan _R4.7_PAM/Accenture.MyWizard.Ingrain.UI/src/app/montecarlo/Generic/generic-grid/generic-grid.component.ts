import { Component, AfterViewInit, Input } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AlertService } from '../../services/alert-service.service';
import { SimulationHeader } from '../../shared/component/simulation-header/simulation-header.service';
import { GridDataService } from '../../ADSP/grid-table/grid-data.service';
import { ApiCore } from '../../services/api-core.service';


@Component({
  selector: 'app-generic-grid',
  templateUrl: './generic-grid.component.html',
  styleUrls: ['./generic-grid.component.scss'],
})
export class GenericGridComponent implements AfterViewInit {
  @Input() gridTableRefresh = new BehaviorSubject<any>('');
  genericData;
  genericHeader = [];
  genericRow = [];
  genericTarget: string;
  genericUnique: string;
  uniqueIndexed: number;
  targetIndexed: number;
  recordToDelete = [];
  constructor(private message: AlertService, private tableheader: SimulationHeader, private apiCore: ApiCore,
    private gridtable: GridDataService) { }

  ngAfterViewInit() {
    this.gridTableRefresh.subscribe(
      data => {
        this.genericData = [];
        this.gridtable.genericRowData.row = {};
        this.gridtable.genericRowData.column = [];
        this.genericData = data;
        this.genericTarget = null;
        this.genericUnique = null;
        this.setColumnHeader();
        this.setTargetUnique();
        this.setRowData();
        // this.addNewRecord();
        // this.genericRow.pop();
        this.gridtable.genericRowData.row = this.genericRow;
      }
    );
  }

  private setColumnHeader() {
    this.genericHeader = [];
    const headerName = Object.keys(this.genericData.Features);
    this.genericHeader = headerName;
    this.gridtable.genericRowData.column = headerName;
  }

  private setTargetUnique() {
    this.genericTarget = this.genericData.TargetColumn;
    this.apiCore.paramData.targetcolumn = this.genericData.TargetColumn;
    this.genericUnique = this.genericData.UniqueIdentifierName;
    this.setUniqueIdentifierIndex();
  }

  private setRowData() {
    this.genericRow = [];
    const rowWiseData = [];
    for (const headerName in this.genericData.Features) {
      if (this.genericData.Features) {
        const headerNameValue = this.genericData.Features[headerName];
        for (let value = 0; value < headerNameValue.length; value++) {
          if (headerNameValue) {
            if (rowWiseData.hasOwnProperty(value)) {
              rowWiseData[value].push(headerNameValue[value]);
            } else {
              rowWiseData[value] = [headerNameValue[value]];
            }
          }
        }

        // for (let value = 0; value < headerNameValue.length; value++) {
        //   const data1 = {};
        //   const key = headerName + '_' + value;
        //     if (headerNameValue) {
        //       if ( rowWiseData.hasOwnProperty(value)) {
        //         data1[key] =  headerNameValue[value];
        //         rowWiseData[value].push(data1);
        //        } else {
        //          data1[key] =  headerNameValue[value];
        //         rowWiseData[value] = [data1];
        //        }
        //     }
        //   }
      }
    }
    // console.log(rowWiseData);
    this.genericRow = rowWiseData;
    this.gridtable.genericRowData.row = this.genericRow;
    // this.checkColor();
  }

  public addNewRecord() {
    const rowTobeAdded = [];
    if (this.genericRow.length < 20) {
      const rowSchema = this.genericRow[this.genericRow.length - 1];
      for (const key in rowSchema) {
        if (rowSchema) {
          if (rowSchema[key] === 'Current' || rowSchema[key] === 'Past') {
            rowTobeAdded.push('Past');
          } else {
            rowTobeAdded.push(1);
          }
        }
      }
      this.message.success('New row added');
      this.genericRow.push(rowTobeAdded);
      this.gridtable.genericRowData.row = this.genericRow;
      this.tableheader.setRunSimulationButton(true);
      this.apiCore.paramData.viewSimulationFlag = 'No';
    } else {
      this.message.warning('User can add only 20 rows');
    }
  }

  public deleteRecord(rowIndex: number) {
        this.genericRow.splice(rowIndex, 1);
        this.message.success('Record deleted temporarily. Click on Save to save the changes.');
        // this.message.success('Selected record deleted');
        this.gridtable.genericRowData.row = this.genericRow;
        this.tableheader.setRunSimulationButton(true);
        this.apiCore.paramData.viewSimulationFlag = 'No';
  }

  public selectedRecords(event, rowIndex) {
    if (event.currentTarget.checked) {
      this.recordToDelete[rowIndex] = event.currentTarget.checked;
    } else {
      delete this.recordToDelete[rowIndex];
    }
  }

  public setRowAsCurrentIteration(rowIndex, rowValue) {
   // console.log('past row', this.genericRow[rowValue]);
    if (rowIndex[rowValue] !== 'Current') {
      const currentRow = JSON.parse(JSON.stringify(this.genericRow[0]));
      this.genericRow[0] = JSON.parse(JSON.stringify(this.genericRow[rowValue]));
      this.genericRow[rowValue] = JSON.parse(JSON.stringify(currentRow));
      this.genericRow[0][0] = 'Current';
      this.genericRow[rowValue][0] = 'Past';
      this.tableheader.setRunSimulationButton(true);
      this.apiCore.paramData.viewSimulationFlag = 'No';
    }
  }

  private checkColor() {

    //  this.tableheader.setRunSimulationButton(false);
    // tslint:disable-next-line: forin
    for (const l in this.genericRow) {
      const index = this.genericRow[l].findIndex((data) => {
        return data === 0;
      });
      if (index > -1) {
        this.tableheader.setRunSimulationButton(true);
        break;
      } else {
        this.tableheader.setRunSimulationButton(false);
      }
    }
  }

  public setTarget(name: string) {
    if (name !== this.genericTarget) {
      this.genericTarget = name;
      this.apiCore.paramData.targetcolumn = name;
      this.setUniqueIdentifierIndex();
      this.tableheader.setRunSimulationButton(true);
      this.apiCore.paramData.viewSimulationFlag = 'No';
    }
  }

  showRunSimulationButton() {
    this.tableheader.setRunSimulationButton(true);
    this.apiCore.paramData.viewSimulationFlag = 'No';
  }

  public rowEntered(value, rowindex, columnIndex) {
    this.showRunSimulationButton();
    if (this.apiCore.isSpecialCharacterGeneric(value.target.value) === 0) {
      value = 'invalid'; 
      this.genericRow[rowindex][columnIndex] = value;
      this.gridtable.genericRowData.row = this.genericRow;
      return 0;
    }
    if (this.uniqueIndexed === columnIndex) { value = value.target.value; } else if (this.targetIndexed === columnIndex && rowindex === 0) {
      value = value.target.value;
    } else if (Number(value.target.value) > 99999999) { 
      value = 'invalidRange';
    } else { 
      if ( value.target.value === '') { value = 'invalid'; }
      else { value = ((Number(value.target.value)) || (Number(value.target.value) === 0)) ? value.target.value * 1 : 'invalid'; }
      }
    this.genericRow[rowindex][columnIndex] = value;
    this.gridtable.genericRowData.row = this.genericRow;
    // this.tableheader.disableSaveAs = true;
    // this.tableheader.setRunSimulationButton(true);
    // this.apiCore.paramData.viewSimulationFlag = 'No';
  }

  /*   setUniqueIdentifier(name: string) {
      this.genericUnique = name;
    } */


  // Optimizing ngFor using trackBy

  public trackByRowData(index: number, item) {
    if (!item) { return null; }
    return item[index - 1];
  }

  public trackByRowValue(index: number, item) {
    if (!item) { return null; }
    return item[index - 1];
  }

  private setUniqueIdentifierIndex(): void {
    const index = this.genericHeader.findIndex(x => (x === this.genericUnique))
    this.uniqueIndexed = index;
    const index1 = this.genericHeader.findIndex(x => (x === this.genericTarget))
    this.targetIndexed = index1;
  }
}
