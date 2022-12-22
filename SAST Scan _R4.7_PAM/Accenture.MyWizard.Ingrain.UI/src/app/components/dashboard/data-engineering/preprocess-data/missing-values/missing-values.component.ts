import { Component, OnInit, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

@Component({
  selector: 'app-missing-values',
  templateUrl: './missing-values.component.html',
  styleUrls: ['./missing-values.component.scss']
})
export class MissingValuesComponent implements OnInit, OnChanges {

  @Input() missingValues;
  @Input() applyFlag;
  // @Input() missingValueAttributes;
  @Input() TextTypeColumnList;

  @Input() FeatureDataTypes; // Missing DataTypes
  @Input() readOnly;


  missingValueAttributes = [];
  missingValueDropDown;
  missingValueOptions = [];
  defaultMissingValueOptions = {};
  missingValuesCustomValue: {} = {};
  dataToServer = {
    'MissingValues': {},
  };

  @Output() setDatatoserverOnChange = new EventEmitter();
  @Output() setDatatoserverMissingValue = new EventEmitter();

  constructor(private cs: CoreUtilsService) { }

  ngOnInit() {
  }

  ngOnChanges() {
    // console.log(this.missingValues);
    this.initMissingValues();
  }

  onChange(tabName, attribute, selectedOption, inputValue) {
    // (tabName, attribute, selectedOption, inputValue)
    // Missing DataTypes
    if (selectedOption === 'CustomValue') {
      if (inputValue !== null && inputValue !== undefined) {
        const valid = this.cs.isSpecialCharacter(inputValue);
        if ( valid === 0) {
         return 0;
        }
        if ( this.FeatureDataTypes[attribute] === 'int64' || this.FeatureDataTypes[attribute] === 'float64') {
          inputValue = inputValue * 1;
        }
        const params = [tabName, attribute, selectedOption, inputValue];
        this.setDatatoserverOnChange.emit(params);
        const customFlagParams = [tabName, attribute, "CustomFlag", "True"];
        this.setDatatoserverOnChange.emit(customFlagParams);
      }
    } else {
      this.defaultMissingValueOptions[attribute] = "";
      const params = [tabName, attribute, selectedOption, null];
      this.setDatatoserverOnChange.emit(params);
    }
  }

  initMissingValues() {

    for (const i in this.missingValues) {

      if (this.missingValues.hasOwnProperty(i)) {
        this.missingValueAttributes.push(i);

        this.missingValueOptions[i] = Object.keys(this.missingValues[i]).sort(function (a, b) {
          if (a !== 'CustomValue' && b !== 'CustomValue') {
            const nameA = a.toUpperCase(); //  ignore upper and lowercase
            const nameB = b.toUpperCase(); //  ignore upper and lowercase
            if (nameA < nameB) {
              return -1;
            }
            if (nameA > nameB) {
              return 1;
            }
            return 0;
          }
        }).filter(item => {
          return item !== 'ChangeRequest' && item !== 'PChangeRequest' && item !== 'None' && item !== 'CustomFlag';
        });

        this.missingValueOptions[i].push('None');

        this.dataToServer.MissingValues[i] = {};
        // this.dataToServer.MissingValues[i][this.missingValueOptions[i][0]] = 'True';
        //  console.log(this.dataToServer.MissingValues);
        this.defaultMissingValueOptions[i] = this.getKeyByValue(this.missingValues[i], 'True');
        if (this.cs.isNil(this.defaultMissingValueOptions[i])) {
          this.dataToServer.MissingValues[i][this.missingValueOptions[i][0]] = 'True';
        } else {
          this.dataToServer.MissingValues[i][this.defaultMissingValueOptions[i]] = 'True';
        }
        // this.dataToServer.MissingValues[i][this.defaultMissingValueOptions[i]] = 'True';
        if (this.defaultMissingValueOptions[i] === 'None') {
          this.defaultMissingValueOptions[i] = '';
        }
        // If CustomFlag is True, dropdown selected option must be CustomValue
        if (this.defaultMissingValueOptions[i] === 'CustomFlag') {
          this.defaultMissingValueOptions[i] = 'CustomValue';
          this.missingValuesCustomValue[i] = this.missingValues[i]['CustomValue'];
          this.missingValueOptions[i]['CustomValue'] = this.missingValuesCustomValue[i];
          this.dataToServer.MissingValues[i]['CustomValue'] = this.missingValues[i]['CustomValue'];
        } else {
          if (this.FeatureDataTypes[i] === 'float64' || this.FeatureDataTypes[i] === 'int64') {
            this.missingValuesCustomValue[i] = 1;
          } else {
            this.missingValuesCustomValue[i] = '';
          }
        }

        // if ( this.FeatureDataTypes[i] === 'float64' || this.FeatureDataTypes[i] === 'int64') {
        //     this.missingValuesCustomValue[i] = 1;
        //  // this.missingValuesCustomValue[i] = this.defaultMissingValueOptions[i];
        // } else {
        // this.missingValuesCustomValue[i] = this.missingValues[i]['CustomValue'];
        // }
        // this.missingValueOptions[i]['CustomValue'] = this.missingValuesCustomValue[i];
      }

    }

    this.setDatatoserverMissingValue.emit(this.dataToServer.MissingValues);
  }

  // getSelectedmissingvalue(attribute, option) {
  //   if(this.missingValues[attribute][option] === 'True'){
  //    return true;}
  // }

  getKeyByValue(object, value) {
    if (!object) {
      return '';
    }

    delete object.ChangeRequest;
    delete object.PChangeRequest;
    const result = Object.keys(object).filter(key => object[key] === value);
    return result[0];
  }


  // Missing DataTypes
  inputOnlyIntegers( attribute , value ) {
      if ( value.includes('.')) {
      this.missingValuesCustomValue[attribute] = ( value.substring(0, value.indexOf('.'))) ;
      } else {
        this.missingValuesCustomValue[attribute] = value;
      }
      this.onChange('MissingValues', attribute, 'CustomValue', this.missingValuesCustomValue[attribute] * 1);
  }

}
