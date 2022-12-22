import {
  Component, OnInit, ElementRef, Renderer2,
  EventEmitter, Output, Input, ViewChild, AfterViewInit, OnDestroy
} from '@angular/core';
import { FormGroup, FormArray, FormControl } from '@angular/forms';
import * as _ from 'lodash';
import { switchMap, catchError } from 'rxjs/operators';
import { empty, throwError, timer, of, pipe, EMPTY } from 'rxjs';
import { Router } from '@angular/router';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { AddFeatureService } from './add-feature.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
// import { elementClassProp } from '@angular/core/src/render3';


@Component({
  selector: 'app-add-feature-board',
  templateUrl: './add-feature-board.component.html',
  styleUrls: ['./add-feature-board.component.scss'],
})
export class AddFeatureBoardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('addbutton', { static: false }) addbutton: ElementRef;
  @ViewChild('addbuttonDiv', { static: false }) addbuttonDiv: ElementRef;

  @Output() featureCreated = new EventEmitter<any>(); // Emit the Feature to Preprocessed component
  @Input() columnList; // Used for column dropdown
  @Input() IsCustomColumnSelected; // Used for Custom Target Column
  @Input() targetColumn; // Used for Custom Target Column
  @Input() featureDataTypes; // Used for Capturing the data type of each column dropdown
  @Input() allColumns; // Used for Restricting user to give feature name as column name
  @Input() featuresAddedbyUser; // Prepopulated Add Feature  [Bug 709222, 708531, 708241 ]
  featuresAddedbyUserWithOutClose = {}; // Used for Retain Add feature
  newFeatureForm: FormGroup; // Main form group
  isButton: Boolean = true; // Not Used
  attributeNameIsExist: Boolean = false; // Used to check Feature name :: duplicate or not
  featureNameToMatch = {}; // Used to check Feature name :: duplicate or not
  OperationType = 'OperationType'; // Used for Request Payload
  ColDrp = 'ColDrp'; // Used for Request Payload
  Value = 'Value'; // Used for Request Payload
  Value2 = 'Value2'; // Used for Request Payload
  Operator = 'Operator'; // Used for Request Payload
  Column = 'Column'; // // Used for Request Payload Only to send for Operation Type Date & Time

  value_check: Array<String> = []; // Used for Value Pop :: check box
  isSelectedTrueorFalse: Boolean = true; // Used for Value Pop :: True False tab
  valuePopUpOperationType = {}; // Used for Value Pop :: Operation Type selected
  valueData = [{ 'true': {}, 'false': {} }, { 'true': {}, 'false': {} },
  { 'true': {}, 'false': {} }, { 'true': {}, 'false': {} }, { 'true': {}, 'false': {} }]; // Used for Value Pop :: For 5 Features
  showValuePopUp = [0, 0, 0, 0, 0]; // Used for Value Pop :: Hide/Show Value Pop
  optionsList = {}; // Used for Showing the Column dropdown
  optionsListForValuePopUp = {};  // Used for Showing the Column dropdown in Value Pop Up

  correlationId: string; // Used for fetching the Unique Value for selected column dropdown
  uniqueValuesColumns; // Used to show the Unique Values
  uniqueValueDropDownList = {}; // Used for Showing the Column dropdown, if Operation is 'Values'
  uniqueValueDropDownListForValuePop = {}; // Used for Showing the Column dropdown, if Operation is 'Values'

  addManualFeatureRequestPayload = {}; // Payload after all operation are done
  totalInputColumn = []; // NOT in use , python will update existing attribute All the column // Bug 708241 
  defaultDataTimeList: Array<string> = []; // Date Time Datatypes
  datetypecolumnList = []; // Add Feature for date & Time
  numbericcolumnList = []; // Add Feature for Numberic Operation
  texttypecolumnList = []; // Add Feature for Text
  isOnlySpecialChar // Used for Special character check
  valueNotEntered = false; // Used for replace and substring
  substringError = false; // Used for replace and substring
  createFeatureSub: any; // Used for retry calling
  timerSubscription: any; // Used for retry calling

  constructor(private renderer: Renderer2, private ns: NotificationService, private dataEngineeringSerivce: DataEngineeringService
    , private ls: LocalStorageService, private ps: ProblemStatementService, private router: Router, private appUtilsService: AppUtilsService,
    private addFeatureService: AddFeatureService, private coreUtilsService: CoreUtilsService) {
    this.newFeatureForm = new FormGroup({
      attribute: new FormArray([this.addAttributeGroup()])
    });
  }

  ngOnInit() {
    // this.appUtilsService.loadingStarted();
    this.correlationId = this.ls.getCorrelationId();
    this.defaultDataTimeList = ['Year', 'Month', 'Day', 'Hour', 'Minute', 'Second', 'Weekday'];
    this.datetypecolumn();

    this.dataEngineeringSerivce.getUniqueValuesOfEachColumns(this.correlationId).subscribe(data => {
      if (data) { this.uniqueValuesColumns = data.ColumnsUniqueValues; }
    }, error => { // this.ns.error('Data is not availalbe for unique values');
    });
  }

  ngAfterViewInit() {
    this.totalInputColumn = this.allColumns;  // Prepopulated Add Feature  [Bug 709222, 708531, 708241 ]
    if (this.featuresAddedbyUser && this.featuresAddedbyUser !== '') {
      this.prepopulateFeaturesAddedByUser();
    }
    if ( this.IsCustomColumnSelected === 'True') { this.firstFeatureIsCustomCreated(); }
  }

  // Add new attribute
  addAttributeGroup() {
    return new FormGroup({
      featureName: new FormControl({ value: '', disabled: false }),
      defaultBlockControls: new FormArray([
        this.defaultContolsGroup()
      ])
    });
  }

  // Default Control with Operator
  defaultContolsGroup() {
    return new FormGroup({
      OperationType: new FormControl(''),
      columnNameInput: new FormControl(),
      columnNameInput2: new FormControl(),
      columnDropdown: new FormControl(''),
      columnNames: new FormControl(''),
      // inputBox: new FormControl(''),
      Operator: new FormControl(''),
      SelectedUniqueValue: new FormControl('')
    });
  }

  getDefaultAttributeArray(form) {
    return form.controls.attribute.controls;
  }

  getDefaultControl(form) {
    return form.controls.defaultBlockControls.controls;
  }

  // To create new attribute block
  repeatAttributeBlock(featureIndex: number) {
    const control = <FormArray>this.newFeatureForm.get('attribute');
    const value: string = this.newFeatureForm.get('attribute')['controls'][featureIndex]['controls']['featureName'].value;

    if (value === '' || null || control.length > 4) {
      if (value === '' || null) {
        this.ns.warning('Please enter attribute name, and select drop downs');
      } else {
        this.ns.warning('You can only add upto 5 feature name blocks');
      }
    } else {
      const key = control.length + '';
      if (this.isAttributeNameExist(value, featureIndex)) {
        this.ns.warning('Attribute name already exist.');
      } else {
        control.push(this.addAttributeGroup());
      }
    }
  }

  // Check if already attribute name exist
  isAttributeNameExist(name: string, featureIndex: number) {
    // if (featureIndex === 0) {
    //   if (this.totalInputColumn.indexOf(name) > -1) { //  Bug 708241
    //     return true;
    //   } else {
    //     return false;
    //   }
    // }
    let returnValue = false;
    for (let loop = featureIndex - 1; loop >= 0; loop--) {
      if (name === this.featureNameToMatch[loop]) { returnValue = true; }
    }

    // if (this.totalInputColumn.indexOf(name) > -1) {
    //   returnValue = true;
    // }
    return returnValue;
  }

  datetypecolumn() {
    const date = [];
    const texttypelist = [];
    const numberlist = [];
    for (const key in this.featureDataTypes) {
      if (this.featureDataTypes[key] === 'datetime64[ns]') {
        date.push(key);
      } else if (this.featureDataTypes[key] === 'Text') {
        texttypelist.push(key);
      } else if (this.featureDataTypes[key] === 'float64' || this.featureDataTypes[key] === 'int64' ) {
        numberlist.push(key);
      }
    }
    this.datetypecolumnList = date;
    this.texttypecolumnList = texttypelist;
    this.numbericcolumnList = numberlist;
  }

  // To Delete/Clear the attribute
  removeSection(featureIndex: number) {
    // Handling the value check pop up 
    this.valueData[featureIndex].true = {};
    this.valueData[featureIndex].false = {};
    this.value_check[featureIndex] = 'false';
    this.showValuePopUp[featureIndex] = 0;
    // Handling the value check pop up 
    const control = <FormArray>this.newFeatureForm.get('attribute');
    if (featureIndex !== 0) {
      this.featureNameToMatch[featureIndex] = '';
      this.getFeatureNameInput(featureIndex);
      control.removeAt(featureIndex);
      // this.submitAddFeatureParametersToAPI('showModel');
      return '1';
    } else {
      this.featuresAddedbyUserWithOutClose = {};
      control.removeAt(0);
      // this.submitAddFeatureParametersToAPI('showModel');
    }
    if (control.length === 0) {
      control.removeAt(featureIndex);
      this.newFeatureForm = new FormGroup({
        attribute: new FormArray([this.addAttributeGroup()])
      });
      this.addManualFeatureRequestPayload = {};
      this.createFeature(); // For Empty features
      // this.featureCreated.emit([data, 'created']);
    }
  }

  closeFeaturePopUp() {
    this.featureCreated.emit([this.featuresAddedbyUserWithOutClose, 'closepop']);
  }

  // Reset the input
  resetDropDown(i, j, formControlName) {
    this.resetColumnValue(i, j, formControlName);
  }

  // Reset Column Value
  resetColumnValue(i: number, j: number, columnName: string) {
    this.newFeatureForm.get('attribute')['controls'][i]
      .get('defaultBlockControls')['controls'][j]['controls'][columnName].reset({
        columnName: ''
      });
  }

  // To add new operation row
  addNewOperationRow(featureIndex: number, e: any) {
    const control = <FormArray>this.newFeatureForm.get('attribute')['controls'][featureIndex].get('defaultBlockControls');
    control.push(this.defaultContolsGroup());
    this.isButton = false;
    if (e) {
      this.renderer.addClass(e.currentTarget.parentElement, 'd-none');
      this.renderer.addClass(e.target.parentElement, 'd-none');
    }
  }

  removeOperationRow(featureIndex: number, operationRowIndex: number, e: any) {
    console.log(operationRowIndex);
    console.log(featureIndex);
    const control = <FormArray>this.newFeatureForm.get('attribute')['controls'][featureIndex].get('defaultBlockControls');
    if ( operationRowIndex != 0) control.removeAt(operationRowIndex);
  }

  // Store the current feature
  getFeatureNameInput(featureIndex: number) {
    const i = featureIndex;
    this.attributeNameIsExist = false;
    let value: string = this.getCurrentFeatureName(i);
    value = value.trim();
    if (value !== '') {
      if (this.isAttributeNameExist(value, featureIndex)) {
        // this.ns.warning('Attribute name already exist.');
        this.attributeNameIsExist = true;
      } else {
        this.featureNameToMatch[i] = value || {};
      }
    } else {
      // this.ns.error('Please enter attribute name');
    }
  }

  // Get Selected Column details and send to payload
  getSelectedColumn(operationRowIndex: number, featureIndex: number, source?: string) {
    const j = operationRowIndex; const i = featureIndex; const formControlName = 'columnDropdown';
    const valueofAttributeName: string = this.getCurrentFeatureName(i);
    if (valueofAttributeName === '' || null) {
      this.ns.error('Please enter attribute name');
      this.resetDropDown(i, j, formControlName);
    } else {
      const value: string = source === 'dropdown' ? this.getControlValue(i, j, 'columnDropdown')
        : this.getControlValue(i, j, 'columnNameInput');
      const operationValue: string = this.getControlValue(i, j, this.OperationType); // TODO Check Date&TIME api send Column and ColDrp
      const enteredName: string = this.featureNameToMatch[i];
      if (operationValue === 'Values') {
        this.showUniqueValueForColumnSelected(i, j, value);
      }
    }
  }

  getSelectedOperator(event, operationRowIndex: number, featureIndex: number) {
    const j = operationRowIndex; const i = featureIndex; const formControlName = this.Operator;
    const valueofAttributeName: string = this.getCurrentFeatureName(i);
    if (valueofAttributeName === '' || null) {
      this.ns.error('Please enter attribute name');
      this.resetDropDown(i, j, formControlName);
    } else {
      const value: string = this.getControlValue(i, j, this.Operator);
      if (this.addFeatureService.operatorTypeIsComparing(value)) {
        this.showValuePopUp[featureIndex] = 1;
        this.value_check[featureIndex] = 'false';
      } else {
        this.value_check[featureIndex] = 'false';
        this.showValuePopUp[featureIndex] = 0;
      }
    }
  }

  // Get Current Feature Value
  getCurrentFeatureName(featureIndex: number) {
    let featureName = this.newFeatureForm.get('attribute')['controls'][featureIndex]['controls']['featureName'].value;
    featureName = featureName.trim();
    this.newFeatureForm.get('attribute')['controls'][featureIndex]['controls']['featureName'].setValue(featureName);
    return featureName.trim();
  }

  // Get Column Value
  getControlValue(i: number, j: number, columnName: string) {
    return this.newFeatureForm.get('attribute')['controls'][i]
      .get('defaultBlockControls')['controls'][j]['controls'][columnName].value;
  }

  // Set Column Value
  // setSelectedColumn($event,featureIndex, operationRowIndex, 'dropdown' )
  setSelectedColumn(data: string, i: number, j: number, columnName: string, controlName?: string) {

    this.newFeatureForm.get('attribute')['controls'][i]
      .get('defaultBlockControls')['controls'][j]['controls'][controlName].setValue(data);
    this.getSelectedColumn(j, i, columnName);
  }

  // Set Value_Check CheckBox
  setValueCheck(value: any, featureIndex: number) {
    this.value_check[featureIndex] = value.checked.toString();
  }

  // True or False Value Selected
  selectedValueCheck(state, featureIndex) { this.isSelectedTrueorFalse = state; }

  // isOperatorRequired
  isOperatorTypeValueRequired(operationRowIndex: number, featureIndex: number) {
    const j = operationRowIndex;
    const i = featureIndex;
    const totalRow = this.newFeatureForm.get('attribute')['controls'][featureIndex]['controls']['defaultBlockControls'].length;
    if (totalRow - 1 > operationRowIndex) {
      return 1;
    } else { return 0; }
  }


  // Set OperationType Value
  setOperationType(data: string, featureIndex: number, operationRowIndex: number) {
    this.newFeatureForm.get('attribute')['controls'][featureIndex]
      .get('defaultBlockControls')['controls'][operationRowIndex]['controls'][this.OperationType].setValue(data);
    const key = featureIndex + '' + operationRowIndex;
    this.optionsList[key] = [];
    this.optionsList[key] = this.showColumnValueBasedOnOperation(data);
    this.newFeatureForm.get('attribute')['controls'][featureIndex]
    .get('defaultBlockControls')['controls'][operationRowIndex]['controls']['columnDropdown'].setValue('');
    this.newFeatureForm.get('attribute')['controls'][featureIndex]
    .get('defaultBlockControls')['controls'][operationRowIndex]['controls']['columnNameInput'].setValue('');
    this.newFeatureForm.get('attribute')['controls'][featureIndex]
    .get('defaultBlockControls')['controls'][operationRowIndex]['controls']['columnNameInput2'].setValue('');
  }

  // Get OperationType Value
  getOperationType(featureIndex: number, operationRowIndex: number) {
    return this.newFeatureForm.get('attribute')['controls'][featureIndex]
      .get('defaultBlockControls')['controls'][operationRowIndex]['controls'][this.OperationType].value;
  }
  // Set OperationType Value For Value Pop Up
  setOperationTypeForValuePopUp(data: string, featureIndex: number) {
    this.valuePopUpOperationType[featureIndex] = data;
    if (this.isSelectedTrueorFalse) {
      this.valueData[featureIndex].true[this.OperationType] = data;
    } else {
      this.valueData[featureIndex].false[this.OperationType] = data;
    }
    const key = featureIndex;
    if (this.IsTextInputDisplayed(this.valueData[featureIndex].true[this.OperationType])) { this.valueData[featureIndex].true[this.Value] = ''; }
    if (this.IsTextInputDisplayed(this.valueData[featureIndex].false[this.OperationType])) { this.valueData[featureIndex].false[this.Value] = ''; }
    this.optionsListForValuePopUp[key] = [];
    // const columnListWithoutDate = this.columnList.filter(val => !this.datetypecolumnList.includes(val)); // Code to remove Date time types columns
    // this.optionsListForValuePopUp[key] = (data === 'Date & Time') ? this.datetypecolumnList : this.columnList; // Date & Time
    
    this.optionsListForValuePopUp[key] = this.showColumnValueBasedOnOperation(data);
    this.valueData[featureIndex].false[this.Value] = '';
    this.valueData[featureIndex].false[this.Value2] = '';

    this.valueData[featureIndex].true[this.Value] = '';
    this.valueData[featureIndex].true[this.Value2] = '';
  }

  // Set Column DropDown Value For Value Pop Up
  setColumnDropDownForValuePopUp(featureIndex: number, colType: string, data: string) {
    this.valueData[featureIndex].true[this.Column] = '';
    this.valueData[featureIndex].false[this.Column] = '';
    if (this.isSelectedTrueorFalse) {
      if (colType === 'Column') {
        this.valueData[featureIndex].true[this.Column] = data;
      } else {
        this.valueData[featureIndex].true[this.ColDrp] = data;
      }
    } else {
      if (colType === 'Column') {
        this.valueData[featureIndex].false[this.Column] = data;
      } else {
        this.valueData[featureIndex].false[this.ColDrp] = data;
      }
    }
    this.showUniqueValueForColumnSelectedForValuePop(featureIndex, data);
  }

  // Set Column DropDown Value For Value Pop Up
  setValueForValuePopUp(featureIndex: number, data: string, value: string) {
    this.valueData[featureIndex].true[value] = '';
    this.valueData[featureIndex].false[value] = '';
    if (this.isSelectedTrueorFalse) {
      this.valueData[featureIndex].true[value] = data;
    } else {
      this.valueData[featureIndex].false[value] = data;
    }
  }

  // close pop up
  closeValuePopUp(key) {
    this.showValuePopUp[key] = 0;
  }

  // Call Manual Feature API
  submitAddFeatureParametersToAPI(section?) {
    this.addManualFeatureRequestPayload = {};
    const control = <FormArray>this.newFeatureForm.get('attribute');
    const recheckedData = {};
    this.valueNotEntered = false;
    this.substringError = false;
    for (let index = 0; index < control.length; index++) {
      const featureData = control.value[index];
      if ( this.IsCustomColumnSelected === 'True') { control.value[0].featureName = this.targetColumn } 
      const featureName1 = control.value[index].featureName;
      const regexSplChars = /[@!#$%^&*<>,?/|}{~:]/; // Bug 719081 
      const isOnlySpecialChar = regexSplChars.test(featureName1);
      if (featureName1 === '' || isOnlySpecialChar || this.attributeNameIsExist) {
        if (featureName1 === '') { this.ns.error('Please enter attribute name'); }
        if (isOnlySpecialChar) { this.ns.error('No special characters with spaces allowed in text field.'); }
        if (this.attributeNameIsExist) { this.ns.warning('Attribute name already exist.'); }
        return 0;
      }
      const sendCopyFeatureFormData = {};
      sendCopyFeatureFormData[featureData.featureName] = featureData.defaultBlockControls;
      const checkAllMandatoryField = this.addFeatureService.verifyRequiredFields(sendCopyFeatureFormData);
      if (checkAllMandatoryField === 0) {
        return 0;
      }
      recheckedData[featureName1] = this.verifyData(sendCopyFeatureFormData, index);
    }

    if (this.isOnlySpecialChar) { this.ns.error('No special characters with spaces allowed in text field.'); return 0; }
    if (this.valueNotEntered) { this.ns.error('Enter the required values'); return 0; }
    if (this.substringError) { this.ns.error('Substring inputs are incorrect'); return 0; }

    const recheckDataParsing = {};
    for (let index = 0; index < control.length; index++) {
      const featureName1 = control.value[index].featureName;
      recheckDataParsing[featureName1] = JSON.parse(recheckedData[featureName1]);
      if (this.value_check[index] === 'true') {
        recheckDataParsing[featureName1].value_check = 'true';
        recheckDataParsing[featureName1].value = this.valueData[index];

        if (this.valueData[index]['true'].hasOwnProperty('OperationType') && this.valueData[index]['true'].hasOwnProperty('ColDrp')
          && this.valueData[index]['false'].hasOwnProperty('OperationType') && this.valueData[index]['false'].hasOwnProperty('ColDrp')) {
          const trueOp = this.valueData[index]['true']['OperationType'];
          const falseOp = this.valueData[index]['true']['OperationType'];
          if (this.IsTextInputDisplayed(trueOp) && !this.addFeatureService.isTextEnteredOrNot(this.valueData[index]['true'])
          && !this.addFeatureService.isTextEnteredOrNotSubstring(this.valueData[index]['true'])) {
            this.ns.error('Fill required True/False values');
            return 0;
          }
          if (this.IsTextInputDisplayed(falseOp) && !this.addFeatureService.isTextEnteredOrNot(this.valueData[index]['false'])
          && !this.addFeatureService.isTextEnteredOrNotSubstring(this.valueData[index]['false'])) {
            this.ns.error('Fill required True/False values');
            return 0;
          }
          if (trueOp === 'substring' && !this.addFeatureService.isTextEnteredOrNot(this.valueData[index]['true'])
          && !this.addFeatureService.isTextEnteredOrNotSubstring(this.valueData[index]['true'])) {
            this.ns.error('Fill required True/False values');
            return 0;
          }
          if (falseOp === 'substring' && !this.addFeatureService.isTextEnteredOrNot(this.valueData[index]['false'])
          && !this.addFeatureService.isTextEnteredOrNotSubstring(this.valueData[index]['false'])) {
            this.ns.error('Fill required True/False values');
            return 0;
          }
        } else {
          this.ns.error('Fill required True/False values');
          return 0;
        }
      }
    }

    this.addManualFeatureRequestPayload = recheckDataParsing;
    this.featuresAddedbyUserWithOutClose = this.addManualFeatureRequestPayload;
    this.createFeature();
  }
  ;
  // Show list of all unique value as per selected column
  showUniqueValueForColumnSelected(featureIndex, operationRowIndex, column: string) {
    if (this.getOperationType(featureIndex, operationRowIndex) === 'Values') {
      if (this.uniqueValuesColumns === undefined) {
        this.dataEngineeringSerivce.getUniqueValuesOfEachColumns(this.correlationId).subscribe(data => {
          if (data) {
            this.uniqueValuesColumns = data.ColumnsUniqueValues;
            const numberIndexes = Object.keys(this.uniqueValuesColumns);
            for (let x = 0; x < numberIndexes.length; x++) {
              if (this.uniqueValuesColumns[numberIndexes[x]].ColumnName === column) {
                const key = featureIndex + '' + operationRowIndex;
                // const numberIndexesOfUniqueValue = Object.keys(this.uniqueValuesColumns);
                this.uniqueValueDropDownList[key] = {};
                this.uniqueValueDropDownList[key] = this.convertJSONToArray(this.uniqueValuesColumns[numberIndexes[x]].UniqueValue);
              }
            }
          }
        }, error => {
          // this.ns.error('Data is not availalbe for unique values');
        });
      } else {
        const numberIndexes = Object.keys(this.uniqueValuesColumns);
        for (let x = 0; x < numberIndexes.length; x++) {
          if (this.uniqueValuesColumns[numberIndexes[x]].ColumnName === column) {
            const key = featureIndex + '' + operationRowIndex;
            // const numberIndexesOfUniqueValue = Object.keys(this.uniqueValuesColumns);
            this.uniqueValueDropDownList[key] = {};
            this.uniqueValueDropDownList[key] = this.convertJSONToArray(this.uniqueValuesColumns[numberIndexes[x]].UniqueValue);
          }
        }
      }
    }
  }

  // Show list of all unique value as per selected column -  For Value Popup
  showUniqueValueForColumnSelectedForValuePop(featureIndex, column: string) {
    if (this.valuePopUpOperationType[featureIndex]) {
      if (this.uniqueValuesColumns === undefined) {
        this.dataEngineeringSerivce.getUniqueValuesOfEachColumns(this.correlationId).subscribe(data => {
          if (data) {
            this.uniqueValuesColumns = data.ColumnsUniqueValues;
            const numberIndexes = Object.keys(this.uniqueValuesColumns);
            for (let x = 0; x < numberIndexes.length; x++) {
              if (this.uniqueValuesColumns[numberIndexes[x]].ColumnName === column) {
                const key = featureIndex;
                // const numberIndexesOfUniqueValue = Object.keys(this.uniqueValuesColumns);
                this.uniqueValueDropDownListForValuePop[key] = {};
                // tslint:disable-next-line: max-line-length
                this.uniqueValueDropDownListForValuePop[key] = this.convertJSONToArray(this.uniqueValuesColumns[numberIndexes[x]].UniqueValue);
              }
            }
          }
        }, error => {
          // this.ns.error('Data is not availalbe for unique values');
        });
      } else {
        const numberIndexes = Object.keys(this.uniqueValuesColumns);
        for (let x = 0; x < numberIndexes.length; x++) {
          if (this.uniqueValuesColumns[numberIndexes[x]].ColumnName === column) {
            const key = featureIndex;
            // const numberIndexesOfUniqueValue = Object.keys(this.uniqueValuesColumns);
            this.uniqueValueDropDownListForValuePop[key] = {};
            this.uniqueValueDropDownListForValuePop[key] = this.convertJSONToArray(this.uniqueValuesColumns[numberIndexes[x]].UniqueValue);
          }
        }
      }
    }
  }

  convertJSONToArray(uniqueValue) {
    const returnArray = [];
    const numberIndexesOfUniqueValue = Object.keys(uniqueValue);
    for (let x = 0; x < numberIndexesOfUniqueValue.length; x++) {
      returnArray.push(uniqueValue[numberIndexesOfUniqueValue[x]]);
    }
    return returnArray;
  }

  setSelectedUniqueValue(data, featureIndex, operationRowIndex) {
    this.newFeatureForm.get('attribute')['controls'][featureIndex]
      .get('defaultBlockControls')['controls'][operationRowIndex]['controls']['SelectedUniqueValue'].setValue(data);
  }

  // Reset Column Value
  resetControl(featureIndex, operationRowIndex, controlName, columnName) {
    this.setSelectedColumn('', featureIndex, operationRowIndex, columnName, controlName);
  }

  prepopulateFeaturesAddedByUser() {

    this.featuresAddedbyUserWithOutClose = JSON.parse(JSON.stringify(this.featuresAddedbyUser));
    const featureNames = Object.keys(this.featuresAddedbyUser);
    const numberOfFeatures = featureNames.length;
    // To create dummy block
    this.createEmptyFeatureEntries(numberOfFeatures);
    // Looping through data and set control values
    for (let featureIndex = 0; featureIndex < numberOfFeatures; featureIndex++) {
      const featureDataWithOperation = this.featuresAddedbyUser[featureNames[featureIndex]];
      const totalOperation = Object.keys(featureDataWithOperation);
      for (let operationRowIndex = 0; operationRowIndex < totalOperation.length; operationRowIndex++) {
        const operationRowIndexData = featureDataWithOperation[totalOperation[operationRowIndex]];
        const keys = Object.keys(operationRowIndexData);

        if (operationRowIndex === 0 && operationRowIndexData.hasOwnProperty(this.OperationType)) {
          for (let i = 0; i < keys.length; i++) {
            const keysTypeOfOperation = keys[i];
            this.setControl(featureIndex, operationRowIndex, keysTypeOfOperation, operationRowIndexData[keysTypeOfOperation]);

          }
        } else if (operationRowIndex >= 1 && operationRowIndexData.hasOwnProperty(this.OperationType)) {
          for (let i = 0; i < keys.length; i++) {
            const keysTypeOfOperation = keys[i];
            this.setControl(featureIndex, operationRowIndex, keysTypeOfOperation, operationRowIndexData[keysTypeOfOperation]);
          }
        }
        // Set the control for value check pop up
        if (featureDataWithOperation.hasOwnProperty('value_check')) {
          this.value_check[featureIndex] = featureDataWithOperation['value_check'].toString();

          this.setOperationTypeForValuePopUp(featureDataWithOperation['value'].true, featureIndex);
          this.valueData[featureIndex].false[this.OperationType] = featureDataWithOperation['value'].false.OperationType;
          this.valueData[featureIndex].true[this.OperationType] = featureDataWithOperation['value'].true.OperationType;
          // set ColumnDropDown For value check pop up
          this.valueData[featureIndex].true[this.ColDrp] = featureDataWithOperation['value'].true.ColDrp;
          this.valueData[featureIndex].false[this.ColDrp] = featureDataWithOperation['value'].false.ColDrp;

          // set Value For value check pop up
          if (this.IsTextInputDisplayed(featureDataWithOperation['value'].false.OperationType)) {
            if (featureDataWithOperation['value'].false.OperationType === 'replace') {
              this.valueData[featureIndex].false[this.Value2] = featureDataWithOperation['value'].false.Value2;
            }
            this.valueData[featureIndex].false[this.Value] = featureDataWithOperation['value'].false.Value;
          }

          if (this.IsTextInputDisplayed(featureDataWithOperation['value'].true.OperationType)) {
            if (featureDataWithOperation['value'].false.OperationType === 'replace') {
              this.valueData[featureIndex].false[this.Value2] = featureDataWithOperation['value'].false.Value2;
            }
            this.valueData[featureIndex].true[this.Value] = featureDataWithOperation['value'].true.Value;
          }

          if (featureDataWithOperation['value'].false.OperationType === 'substring') {
            this.valueData[featureIndex].false[this.Value] = featureDataWithOperation['value'].false.Value;
            this.valueData[featureIndex].false[this.Value2] = featureDataWithOperation['value'].false.Value2;
          }
          if (featureDataWithOperation['value'].true.OperationType === 'substring') {
            this.valueData[featureIndex].true[this.Value] = featureDataWithOperation['value'].true.Value;
            this.valueData[featureIndex].true[this.Value2] = featureDataWithOperation['value'].true.Value2;
          }
        } else {
          this.closeValuePopUp(featureIndex);
        }
      }
    }
  }

  /**
   * To set the input data based on params passed
   * @param featureIndex - nth feature index
   * @param operationRowIndex - nth row index for featureIndex
   * @param controlName - type of input
   * @param data - data of input
   * @param source - Optional for dropdowns
   */
  setControl(featureIndex, operationRowIndex, controlName, data, source?) {
    const defaultBlockControls = this.newFeatureForm.get('attribute')['controls'][featureIndex]
      .get('defaultBlockControls')['controls'][operationRowIndex]['controls'];
    if (controlName === 'OperationType') {
      this.setOperationType(data, featureIndex, operationRowIndex);
    }

    if (controlName === 'Column') {
      defaultBlockControls['columnNames'].setValue(data); // Date & Time
    }

    if (controlName === 'ColDrp') {
      controlName = 'columnDropdown';
      defaultBlockControls[controlName].setValue(data);
      const currentOperation = this.getOperationType(featureIndex, operationRowIndex);
      if (currentOperation === 'Values') {
        this.showUniqueValueForColumnSelected(featureIndex, operationRowIndex, data);
        source = 'dropdown';
      }
      this.getSelectedColumn(operationRowIndex, featureIndex, source);
      // this.setSelectedColumn(data, featureIndex, operationRowIndex, 'dropdown', controlName);
    }
    if (controlName === 'Operator') {
      defaultBlockControls[this.Operator].setValue(data);
      this.getSelectedOperator(data, operationRowIndex, featureIndex);
    }

    if (controlName === 'Value') {
      controlName = 'columnNameInput';
      const currentOperation = this.getOperationType(featureIndex, operationRowIndex);
      if (currentOperation === 'Values') {
        this.setSelectedUniqueValue(data, featureIndex, operationRowIndex);
      } else {
        defaultBlockControls[controlName].setValue(data);
      }
    }

    if (controlName === 'Value2') {
      controlName = 'columnNameInput2';
      defaultBlockControls[controlName].setValue(data);
    }

  }

  createEmptyFeatureEntries(numberOfFeatures) {
    const featureNames = Object.keys(this.featuresAddedbyUser);
    // To Set featureIndex name
    for (let featureIndex = 0; featureIndex < numberOfFeatures; featureIndex++) {
      this.newFeatureForm.get('attribute')['controls'][featureIndex]['controls']['featureName'].setValue(featureNames[featureIndex]);
      this.featureNameToMatch[featureIndex] = featureNames[featureIndex];
      if (featureIndex + 1 < numberOfFeatures) {
        this.repeatAttributeBlock(featureIndex);
      }
      const feautureData = this.featuresAddedbyUser[featureNames[featureIndex]];
      const featureDataWithOperation = JSON.parse(this.addFeatureService.shiftingOperatorData(JSON.parse(JSON.stringify(feautureData))));
      this.featuresAddedbyUser[featureNames[featureIndex]] = featureDataWithOperation;
      const totalOperation = Object.keys(featureDataWithOperation);

      if (totalOperation.includes('value_check')) {
        totalOperation.splice(totalOperation.length - 2, 2);
      }
      for (let operationRowIndex = 0; operationRowIndex < totalOperation.length; operationRowIndex++) {
        if (operationRowIndex + 1 < totalOperation.length) {
          this.addNewOperationRow(featureIndex, null);
        }
      }
    }
  }


  verifyData(formData, featureIndex) {

    const data = formData;
    const featurename = Object.keys(data)[0];
    const operationData = formData[featurename];
    const numberOfOperation = Object.keys(operationData);
    let singleOperation = {};
    let combineOperations = {};
    // combineOperations = {};
    let j = 0;
    for (let i = 0; i < numberOfOperation.length; i++) {

      singleOperation['OperationType'] = operationData[i]['OperationType'];
      singleOperation['Column'] = operationData[i]['columnNames']; // for date & time

      if (singleOperation['OperationType'] !== 'Custom') {
        singleOperation['ColDrp'] = operationData[i]['columnDropdown'];
      } else {
        delete singleOperation['ColDrp'];
      }

      if (this.IsTextInputDisplayed(singleOperation['OperationType'])) {
        if (operationData[i]['columnNameInput']) { singleOperation['Value'] = operationData[i]['columnNameInput'].toString(); }
        if (operationData[i]['columnNameInput2']) { singleOperation['Value2'] = operationData[i]['columnNameInput2'].toString(); }
        const regexSplChars = /[@!#$%^&*<>,?/|}{~:]/;
        this.isOnlySpecialChar = regexSplChars.test(singleOperation['Value'] + '' + singleOperation['Value2']);
        this.valueNotEntered = this.addFeatureService.isTextEnteredOrNot(singleOperation);
        this.substringError = this.addFeatureService.isTextEnteredOrNotSubstring(singleOperation);
      } else {
        delete singleOperation['Value'];
      }

      if (singleOperation['OperationType'] === 'substring') {
        singleOperation['Value'] = Number(operationData[i]['columnNameInput']);
        if (operationData[i]['columnNameInput2']) { singleOperation['Value2'] = Number(operationData[i]['columnNameInput2']); }
        // this.valueNotEntered = this.addFeatureService.isTextEnteredOrNot(singleOperation);
        this.substringError = this.addFeatureService.isTextEnteredOrNotSubstring(singleOperation);
      }

      if (singleOperation['OperationType'] === 'Values') {
        singleOperation['Value'] = operationData[i]['SelectedUniqueValue'];
        this.showUniqueValueForColumnSelected(featureIndex, i, singleOperation['ColDrp']);
      }

      if (operationData[i]['Operator'] !== '') {
        singleOperation['Operator'] = operationData[i]['Operator'];
      } else if (operationData[i]['Operator'] === '') {
        delete singleOperation['Operator'];
      }

      combineOperations[j] = JSON.stringify(singleOperation); // Deep cloning 

      // Handling Operator according feature of compare, non comparing.
      if (this.addFeatureService.operatorTypeIsComparing(singleOperation['Operator']) && singleOperation.hasOwnProperty('Operator')) {
        // Code for Comparing operator
        combineOperations[j] = JSON.parse(combineOperations[j]);
        delete combineOperations[j]['Operator'];
        j++;
        if (i < numberOfOperation.length - 1) {
          combineOperations[j] = { 'Operator': singleOperation['Operator'] };
        }
        j++;
      } else if (!this.addFeatureService.operatorTypeIsComparing(singleOperation['Operator']) && singleOperation.hasOwnProperty('Operator')) {
        // Code for Non-Comparing operator like - Add, Substract, Multiply, Divide.
        combineOperations[j] = JSON.parse(combineOperations[j]);
        // }
        j++;
      }

      // Handled deep cloning as operation may not contain any operator
      if ((i) < numberOfOperation.length && (!singleOperation.hasOwnProperty('Operator'))) {
        combineOperations[j] = JSON.parse(combineOperations[j]);
        j++;
      }
    }

    const operationList = JSON.parse(JSON.stringify(combineOperations));
    const len = Object.entries(operationList).length;

    for (let op = len - 1; op > 0; op--) {
      if (operationList[Number(op) - 1].hasOwnProperty('Operator') && !this.addFeatureService.operatorTypeIsComparing(operationList[Number(op) - 1]['Operator'])) {
        operationList[op]['Operator'] = operationList[Number(op) - 1]['Operator'];
      } else {
        if (operationList[Number(op) - 1].hasOwnProperty('Operator')) {
          delete operationList[Number(op)]['Operator'];
        }
      }
    }

    delete operationList['0']['Operator'];
    combineOperations = JSON.parse(JSON.stringify(operationList));
    return JSON.stringify(combineOperations);
  }

  // End - Prepopulated Add Feature  

  public firstFeatureIsCustomCreated() {
    this.newFeatureForm.get('attribute')['controls'][0]['controls']['featureName'].setValue(this.targetColumn);
    this.newFeatureForm.get('attribute')['controls'][0]['controls']['featureName'].disable();
    this.featureNameToMatch[0] = this.targetColumn;
    this.getCurrentFeatureName(0);
  }

  private createFeature() {
    const postFeature = {
    "correlationId": this.ls.getCorrelationId(),
    "pageInfo": "AddFeature",
    "userId": this.appUtilsService.getCookies().UserId,
    "NewAddFeatures": this.addManualFeatureRequestPayload
    }
    this.appUtilsService.loadingStarted();
    this.createFeatureSub = this.addFeatureService.PostAddFeature(postFeature).pipe(
      switchMap(
        data => {
          let tempData = {};
          if (this.addFeatureService.IsJsonString(data)) {
            const parsedData = JSON.parse(data);
            tempData = parsedData;
          } else if (data.constructor === Object) {
            tempData = data;
          } else if (data.constructor === String) {
            // this.noData = data;
            this.ns.success(data);
          }
          if (tempData.hasOwnProperty('message')) {
            return this.addFeatureService.PostAddFeature(postFeature);
          } else if (tempData.hasOwnProperty('processData')) {
            return of(tempData);
          }
        }
      ),
      catchError(data => {
        if (data.hasOwnProperty('error')) {
          if (data.error['Status'] === 'E') {
            this.appUtilsService.loadingImmediateEnded();
            // this.isLoading = false;
            this.ns.error(data.error['Message']);
          }
        } else {
          this.ns.error('Something went wrong');
        }
        this.appUtilsService.loadingImmediateEnded();
        return EMPTY;
      })
    ).subscribe(
      data => {
        if (data['Status'] === 'P') {
          this.recallAddFeatureAPI();
        } else if (data['Status'] === 'C') {
          this.featureCreated.emit([data, 'created'])
          this.appUtilsService.loadingImmediateEnded();
        } else if (data['Status'] === 'E') {
          this.ns.error(data['Message']);
          if (!this.coreUtilsService.isNil(this.timerSubscription)) {
            this.timerSubscription.unsubscribe();
          }
          this.featureCreated.emit([data, 'error'])
          this.appUtilsService.loadingImmediateEnded();
        }
      }
    );
  }

  private recallAddFeatureAPI() {
    this.timerSubscription = timer(10000).subscribe(() => this.createFeature());
    return this.timerSubscription;
  }

  
  public IsTextInputDisplayed(operationSelected: string): boolean {
    const textToShow = ['Custom', 'Percentage', 'contains', 'matches', 'index', 'startswith', 'endswith', 'regexsearch', 'replace'];
    // const textToShow = ['Custom', 'Percentage'];
    const index = textToShow.findIndex(data => data === operationSelected);
    return (index > -1);
  }

  public showColumnValueBasedOnOperation(operationname) {
    const numberOfOperations = [ 'Max', 'Min', 'Mean', 'Median', 'Sd', 'Sqr', 'Sqrt', 'Cube', 'Cbrt', 'Percentage'];
    const textTypeOperations = ['contains', 'matches', 'index', 'length', 'substring', 'startswith', 'endswith', 'regexsearch', 'replace'];
    const numbericColumn = numberOfOperations.findIndex(data => data === operationname);
    const textTypeColumn = textTypeOperations.findIndex(data => data === operationname);
    if ( operationname === 'Date & Time') {
      return this.datetypecolumnList;
    } else if ( numbericColumn > -1) {
      return this.numbericcolumnList;
    } else if ( textTypeColumn > -1) {
      return this.texttypecolumnList;
    } else {
      return this.columnList;
    }
  }

  public ngOnDestroy() {
    if (!this.coreUtilsService.isNil(this.timerSubscription)) {
    this.timerSubscription.unsubscribe();
    }
  }
}
