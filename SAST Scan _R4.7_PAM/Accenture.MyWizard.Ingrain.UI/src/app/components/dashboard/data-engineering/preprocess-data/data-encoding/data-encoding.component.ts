import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';

@Component({
  selector: 'app-data-encoding',
  templateUrl: './data-encoding.component.html',
  styleUrls: ['./data-encoding.component.scss']
})
export class DataEncodingComponent implements OnInit, OnChanges {

  @Input() dataEncodingAttributes;
  @Input() dataEncoding;
  @Input() targetColumn;
  autobinningcolumns: {} = {};
  @Input() autobinningDisableColumns;
  @Input() categoricalColumns;
  @Output() setDatatoserverDataEncodingOnChange = new EventEmitter();
  @Input() readOnly;

  constructor() { }

  ngOnInit() {
  }

  ngOnChanges() {
    this.prePopulateColumns();
  }

  onChange(tabName, attribute, selectedOption, inputValue) {
    // (tabName, attribute, selectedOption, inputValue)

    if (selectedOption === 'CustomValue') {
      if (inputValue !== null && inputValue !== undefined) {
        const params = [tabName, attribute, selectedOption, inputValue];
        this.setDatatoserverDataEncodingOnChange.emit(params);
      }
    } else {
      const params = [tabName, attribute, selectedOption, null];
      this.setDatatoserverDataEncodingOnChange.emit(params);
    }
  }

  getAutoBinningColumns(columns: any, categoricalColumnslist: any) {
  //  if (columns !== undefined) {
    this.autobinningcolumns = {};
    for (const item in columns) {
    if (categoricalColumnslist.hasOwnProperty(item) === false) {
       this.autobinningcolumns = columns; }
      }
  //  }
}

  prePopulateColumns() {
    for (const item in this.autobinningDisableColumns) {
      if (this.categoricalColumns.hasOwnProperty(item) === false) {
         this.autobinningcolumns = this.autobinningDisableColumns;
      }
    }
  }

}
