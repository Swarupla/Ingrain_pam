import { Injectable } from '@angular/core';
import * as moment from 'moment';
import * as _ from 'lodash';
import { ApiCore } from '../../services/api-core.service';
const _moment = moment;

@Injectable({
  providedIn: 'root'
})
export class GridDataService {

  public rowDataInput = [];
  public minPhaseError = '';
  public SelectedCurrentRelease = '';
  public error = {};
  public adspMainColumn = [];
  public release_name = {};
  public genericRowData = {
    'row': {},
    'column': []
  };
  public genericError = {}

  constructor(private apiCore: ApiCore) { }


  public getRowData(category, categoryList) {
    const default_RowData = [];
    // First Two coloumns
    for (let i = 0; i < category.length; i++) {
      const currentRow = categoryList[category[i]];
      if (this.oneDimensionalData(category[i])) {
        for (let j = 0; j < currentRow.length; j++) {
          const data1 = {};
          if (default_RowData.hasOwnProperty(j)) {
            default_RowData[j][category[i]] = currentRow[j];
          } else {
            data1[category[i]] = currentRow[j];
            default_RowData[j] = data1;
          }
        }
      }
    }


    // Phases columns
    for (let i = 0; i < category.length; i++) {
      const currentRow = categoryList[category[i]];
      if (currentRow) {
        if (!(this.oneDimensionalData(category[i]))) {
          const phases = Object.keys(currentRow);
          for (let j = 0; j < default_RowData.length; j++) {
            const data1 = {};
            if (default_RowData.hasOwnProperty(j)) {
              for (let phase = 0; phase < phases.length; phase++) {
                if (phases[phase] === 'Release Start Date (dd/mm/yyyy)' || phases[phase] === 'Release End Date (dd/mm/yyyy)') {
                  if (categoryList[category[i]][phases[phase]][j] !== null) {
                    if ( categoryList[category[i]][phases[phase]][j] !== 0) {
                    categoryList[category[i]][phases[phase]][j] = categoryList[category[i]][phases[phase]][j].split("-").reverse().join("/");
                    const dateinITC =   _moment(categoryList[category[i]][phases[phase]][j], 'DD/MM/YYYY');
                    default_RowData[j][category[i] + '_' + phases[phase]] = dateinITC['_i'];
                    } else {
                      default_RowData[j][category[i] + '_' + phases[phase]] = 0;
                    }
                  } else {
                    default_RowData[j][category[i] + '_' + phases[phase]] = '01/01/2020';
                  }
                } else {
              
                  default_RowData[j][category[i] + '_' + phases[phase]] = categoryList[category[i]][phases[phase]][j] === "" ? 0 : categoryList[category[i]][phases[phase]][j];
                  // if ( this.apiCore.paramData.InputSelection[phases[phase]] === false ) {
                  //   delete default_RowData[j][category[i] + '_' + phases[phase]];
                  // }
                }
              }
            }
          }
        }
      }
    }
    return default_RowData;
  }


  public setRowData(rowData) {
    this.rowDataInput = [];
    // const totalrowcount = gridApi.getDisplayedRowCount();
    // const allRowData = [];
    // for (let i = 0; i < totalrowcount; i++) {
    //   allRowData.push(gridApi.getDisplayedRowAtIndex(i).data);
    // }
    this.rowDataInput = rowData;
  }

  public oneDimensionalData(category) {
    return (category === 'Release State' || category === 'Release Name');
  }

  public isRequiredMandatory() {
    // console.log(this.adspMainColumn);
    this.adspMainColumn.push('Release End Date');
    this.adspMainColumn.push('Release Start Date');

    for (const key in this.error) {
      const index  = this.adspMainColumn.findIndex( phase => { if(key.includes('_' + phase)) { return true; }})
      if ( index > -1 ) {
       const rowNumber =  Number(key.split("_row=")[1]);
      if ( rowNumber <= this.rowDataInput.length - 1)  {
      if (this.error[key] === 'red' || this.error[key] === 'redDate' || this.error[key] === 'DateError' || this.error[key] === 'NumberLimitRange') {
        if (this.error[key] === 'red') { return 'RedCellPresent'; }
        if (this.error[key] === 'redDate') { return 'RedDateCellPresent'; }
        if (this.error[key] === 'DateError') { return 'EndDate<StartDate'; }
        if (this.error[key] === 'NumberLimitRange') { return 'NumberLimitRange'; }
       }
      }   
     }
    }
  }

  public isReleaseNameMandatory() {
    const array = [];
    const config = {
      'duplicate': false,
      'empty': false
    };
    for (const key in this.release_name) {
      if (this.release_name[key]) {
        if (this.release_name[key].length === 0) { config.empty = true; }
        array.push(this.release_name[key]);
      }
    }
    const uniquevalue = new Set(array);
    config.duplicate = (uniquevalue.size !== array.length);
    return config;
  }
}
