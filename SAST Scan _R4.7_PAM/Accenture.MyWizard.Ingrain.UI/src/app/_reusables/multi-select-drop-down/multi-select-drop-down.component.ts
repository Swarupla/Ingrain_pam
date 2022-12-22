import { Component, Input, Output, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { EventEmitter } from '@angular/core';
import { element } from 'protractor';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-multi-select-drop-down',
  templateUrl: './multi-select-drop-down.component.html',
  styleUrls: ['./multi-select-drop-down.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MultiSelectDropDownComponent implements OnInit {

    displayOptions = false;

  placeHolder = 'Please Select an Option';
  @Input() moduleName;
  @Input() columnData = [];
  @Input() defaultValues;
  @Input() disableDropdown;
  @Input() displayCount;
  defaultDisplayCount = 3;
  @Output() outputData = new EventEmitter();
  // @Output() removeData = new EventEmitter();
  // @Output() selection = new EventEmitter();

  // @Input() minWidthValue;
  @Input() parentName: string;
  @Input() accuracyLabel = [];

  selectedValues = new Set([]);
  searchText = '';
  isChecked: {} = {};
  selectedValue: string;
  isselectall: {} = {};
  ischkall = false;
  selectall = false;
  page = 1;

  dropdownOptions = new FormControl([]);;
  constructor() { 
    this.dropdownOptions = new FormControl([]);
  }

  selectionChanged(data) {
   // console.log(data.value);
   if ( data.value.length === 1 && data.value[0]== 'Select All') {

   } else {
   if ( data.value.length > 0) {
   this.outputData.emit(this.removeDuplicateFromArray(data.value));
   } else {
    this.outputData.emit([]);
   }
  }
  }

   ngOnInit() {
    if ( this.defaultValues && this.defaultValues.length > 0) {
    this.dropdownOptions.setValue(this.defaultValues);
    }
    if ( this.displayCount) {
      this.defaultDisplayCount = this.displayCount;
    }
  }

  unselectOption(name, index, event) {
    console.log(name, index);
    event.stopPropagation();
    let selectedOption = this.removeDuplicateFromArray(this.dropdownOptions.value);
    selectedOption.splice(index,1);
    if ( selectedOption.length === 0) {
      this.dropdownOptions.setValue([]);
      this.outputData.emit([]);
    } else {
    this.dropdownOptions.setValue(selectedOption);
    this.outputData.emit(selectedOption);
    }
    event.preventDefault();
  }

  selectAll(ev){
    if(ev._selected){
     const optionsAdded = this.removeDuplicateFromArray(this.columnData);
     this.dropdownOptions.setValue(optionsAdded);
     this.outputData.emit(optionsAdded);
    ev._selected=true;
    }
    if(ev._selected==false){
      const optionsAdded = this.removeDuplicateFromArray([]);
      this.dropdownOptions.reset();
      // this.dropdownOptions.setValue(optionsAdded);
      this.outputData.emit([]);
    }
  }

  removeDuplicateFromArray(data) {
    // let filteredArray =  new Set([]);
    let filteredArray = new Set(data);
    return Array.from(filteredArray);
  }
}