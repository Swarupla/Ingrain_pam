import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-ingest-data-column-names',
  templateUrl: './ingest-data-column-names.component.html',
  styleUrls: ['./ingest-data-column-names.component.scss']
})
export class IngestDataColumnNamesComponent implements OnInit {

  submitFlag: boolean;
  columnNames = [];
  displaycolumns = [];
  uniquecolumnNames = [];
  oldColumnNames = [];
  filterAttributes = {};
  modelName: string;
  disableSubmitBtn = true;
  selectedAll = false;
  filterAttrsOptionVisible = false;
  filterSelectedOptions = [];
  selectedFilterAttributes: [] = [];
  emptyFilters = false;
  noFilterAttrsSelected = false;
  similaritythresholdVisible = false;
  toprecordsvisible = true;
  thresholdvisible = false;
  rangebind = 40;
  stopWordscluster = '';
  SimilaritySW = [];
  filterAttr = {};
  columnData = {};
  toprecords;
  uniqueid;
  isselectFlag: boolean;
  isfilteravailable = false;
  isidentifierdisable = false; 

  constructor(private _dialogRef: DialogRef, private _dialogConfig: DialogConfig,
    private _notificationService: NotificationService) { }

  ngOnInit() {
    this.isselectFlag = false;
    const columnNamesArray = this._dialogConfig.data['columnNames'];
    const UniqueColumnsArray = this._dialogConfig.data['uniqueColmnNames'];
    if (columnNamesArray?.length === 1) {
       this.isidentifierdisable = true;
    }
    this.modelName = this._dialogConfig.data['modelName'];
    this.oldColumnNames = this._dialogConfig.data['oldColumnNames'];
    this.filterAttributes = this._dialogConfig.data['filterAttribute'];
    columnNamesArray?.forEach(element => {
      this.columnNames.push({ 'name': element, 'checked': false, 'disable': false });
    });
    UniqueColumnsArray?.forEach(element => {
      this.uniquecolumnNames.push({ 'name': element, 'checked': false, 'disable': false });
    });
    this.uniquecolumnNames.unshift({ 'name': 'None', 'checked': false, 'disable': false });
    this.displaycolumns = this.columnNames;
  }

  selectColumnNames(index, isSelectAll?) {
    const ifColChecked = [];
    const ifColCheckedTrue = [];
    const ifColCheckedFalse = [];
    this.isselectFlag = false;
    if (isSelectAll === true) {
      this.columnNames[index].checked = this.selectedAll;
    } else {
      this.selectedAll = this.columnNames.every(function(item: any) {
        return item.checked === true;
      });
    }
    // this.columnNames[index].checked = !this.columnNames[index].checked;
    for (const col in this.columnNames) {
      if (this.columnNames) {
        ifColChecked.push(this.columnNames[col].checked);
      }
    }
    for (const checked in ifColChecked) {
      if (ifColChecked[checked] === true) {
        ifColCheckedTrue.push(ifColChecked[checked]);
      } else if (ifColChecked[checked] === false) {
        ifColCheckedFalse.push(ifColChecked[checked]);
      }
    }
    if (ifColCheckedTrue[0] === true) {
      this.disableSubmitBtn = false;
      // if (this.filterAttributes !== undefined) {
      // this.isselectFlag = true;
      // } else {
      //   this.disableSubmitBtn = false;
      // }
    } else if (ifColCheckedFalse[0] === false) {
      this.disableSubmitBtn = true;
      this.isselectFlag = false;
    }
    // if (this.isselectFlag == true && this.uniqueid !== undefined) {
    //   this.disableSubmitBtn = false;
    // }
  }

  onNext() {
   this.emptyFilters = false;
   let columnFilterFlag = 0;
   this.noFilterAttrsSelected = false;
   if (Object.keys(this.filterAttributes).length !== 0) {
    this.columnNames.forEach(element => {
      let colDataFilterOpts = [];
      let defaultFilterOpts = [];
      if (element.checked == true) {
        if (this.filterAttributes.hasOwnProperty(element.name)) {
          colDataFilterOpts = Object.keys(this.filterAttributes[element.name]);
          this.noFilterAttrsSelected = false;
          columnFilterFlag = 1;
          this.isfilteravailable = true;
          colDataFilterOpts.forEach(key => {
            if (this.filterAttributes[element.name][key] == 'True') {
              defaultFilterOpts.push(key);
            }
          });

          this.filterSelectedOptions.push(
            {
              'name': element.name,
              'options': this.filterAttributes[element.name],
              'columnDataFilterOptions': colDataFilterOpts,
              'defaultFilterOptions': defaultFilterOpts
            });
       } 
        else {
          if (columnFilterFlag === 0) {
          this.noFilterAttrsSelected = true; 
        } else {
           this.noFilterAttrsSelected = false;

        }

        }
     }
    });
   } else {
    this.emptyFilters = true;
   }
   this.filterAttrsOptionVisible = true;
  }

  onSelect() {

    if ((this.filterAttributes !== undefined && Object.keys(this.filterAttributes).length !== 0) || JSON.stringify(this.filterAttributes) == '{}') {
      this.filterSelectedOptions.forEach((element, index) => {
        this.filterAttr[element.name] = element.options;
      });
      this.columnData = {
        'columnNames': this.columnNames,
        'filterAttribute': this.filterAttr
      };
      console.log(this.columnData);
      this.similaritythresholdVisible = true;
    // this._dialogRef.close(this.columnData);
    } else {
      this._dialogRef.close(this.columnNames);
    }  
    
  }

  onClose() {
    this.submitFlag = false;
    this._dialogRef.close(this.submitFlag);
  }

  selectAll() {
    for (let i = 0; i < this.columnNames.length; i++) {
      // this.columnNames[i].checked = this.selectedAll;
      this.selectColumnNames(i, true);
    }
  }

  setFilterAttributes(selectedValues, i) {
    this.selectedFilterAttributes = selectedValues;
    Object.keys(this.filterSelectedOptions[i]["options"]).forEach((ele, ind) => {
      this.filterSelectedOptions[i]["options"][ele] = 'False';
    });
    if (selectedValues.length > 0) {
      selectedValues.forEach((element, index) => {
        this.filterSelectedOptions[i]["options"][element] = 'True';
      });
    }
  }

  removeAttribute(selectedValues) {
    this.selectedFilterAttributes = selectedValues;
  }

  showtoprecords() {
    this.toprecordsvisible = true;
    this.thresholdvisible = false;
    this.rangebind = 40;
  }

  showthreshold() {
    this.toprecordsvisible = false;
    this.thresholdvisible = true;
    this.toprecords = undefined;
  }

  onInputChange(value) {
    this.rangebind = value;
  }

    // Add clustering stop words
addStopWords() {

  if(this.stopWordscluster) {
    if(this.SimilaritySW.includes(this.stopWordscluster)){
      this._notificationService.error('Stop word has been already added to the list');
    } else {
         this.SimilaritySW.push(this.stopWordscluster);
    }
    this.stopWordscluster = '';
 } 
}

removeclusterstopWords(index) {
  this.SimilaritySW.splice(index, 1);
}

onSelectstopwords() {
  let key ='';
  let value;
 
  let uniqueIdentifiername = '';
  if (this.uniqueid !== undefined) {
  if (this.uniqueid.name !== 'None') {
    uniqueIdentifiername = this.uniqueid.name;
  } else {
    uniqueIdentifiername = '';
  }
}

if (this.toprecordsvisible === true) {
  if (this.toprecords === undefined || this.toprecords == null || this.toprecords == ''){
    return this._notificationService.error('Please enter the Top n records values');
  } else if(parseInt(this.toprecords) < 1) {
    return this._notificationService.error('Top n records should have a minimum value of 1');
  }


    key = 'top_n';
    value = this.toprecords;
} else {
   key = 'threshold';
   value = this.rangebind;
}

  this.columnData = {
    'columnNames': this.columnNames,
    'filterAttribute': this.filterAttr,
    'uniqueIdentifier': uniqueIdentifiername,
    'Threshold_TopnRecords': { 'key': key, 'value': value  },
    'StopWords': this.SimilaritySW
  };
  this._dialogRef.close(this.columnData);
}

selectUniqueColumn(value) {   
      this.uniqueid = value;
      this.displaycolumns = this.columnNames;
      this.displaycolumns = this.displaycolumns.filter(item => item.name !== this.uniqueid.name);
}

getStyles(value, minValue, maxValue) {
  const diff = maxValue - minValue;
  const number = value * 1;
  const rangefromMinValue = number - minValue;

  let left = 13;
  left = ((rangefromMinValue / diff) * 100);
  if (left > 88) {
    left = left - 5;
  }
  if (number === maxValue) {
    left = 95;
  }

  const styles = {
    'position': 'relative',
    'left': left + '%',
    'z-index': '1',
    'top': '3px',
    'color': '#10ADD3',
    'font-size': '0.7rem',
    'font-weight': '700'
  };
  return styles;
}

}
