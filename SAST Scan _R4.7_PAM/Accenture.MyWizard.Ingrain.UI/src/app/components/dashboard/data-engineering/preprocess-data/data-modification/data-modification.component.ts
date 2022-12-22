import { Component, OnInit, Input, Output, EventEmitter, ElementRef, Inject, TemplateRef } from '@angular/core';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { UsageTrackingService } from 'src/app/_services/usage-tracking.service';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-data-modification',
  templateUrl: './data-modification.component.html',
  styleUrls: ['./data-modification.component.scss']
})
export class DataModificationComponent implements OnInit {

  @Input() dataModification;
  @Input() problemTypeFlag;
  @Input() modelType;
  @Input() AutoBinningDisable;
  @Input() NumericalAutoBinning;
  @Input() SmoteMultiFlag;
  @Output() setDatatoserverDataModification = new EventEmitter();
  @Output() setDatatoserverDataModificationOnChange = new EventEmitter();
  @Output() setautobinningcolums = new EventEmitter();
  @Output() setautobinningcolumsprepopulate = new EventEmitter();
  @Output() binningdata = new EventEmitter();

  dataModificationFeatures: any;
  // featuresAddedbyUser: any;
  autoBinning: any;
  autoBinningDisableColumns: any;
  interpolationDefaultValue: any;
  interpolationItems = ['Linear', 'Cubic', 'Nearest', 'Zero', 'sLinear', 'Quadratic'];
  dataModificationAttributes = [];
  allColumnMappingArray: any = [];


  defaultModificationOptions = {};
  defaultModificationOptionsSkewness = {};
  dataModificationOutlierOptions: {} = {};
  dataModificationSkewnessOptions: {} = {};
  dataModificationRemoveColumnOptions: {} = {};


  dataModificationSkewnessCustomValue: {} = {};
  dataModificationOutlierCustomValue: {} = {};
  dataToServer = {
    'DataModification': {
      'Skewness': {},
      'Outlier': {},
      'binning': {},
      'RemoveColumn': {}
      // 'Interpolation': {}
    }
  };
  autobinningcolumns: string[] = [];

  notcheckbinning: boolean;
  smoteflag: boolean;
  binningcolums: {} = {};
  categoricalcolumns: any;

  autobinningcolumsitem: {} = {};
  payloadautobinningcolumsitem: {} = {};
  radioTextBox: {} = {};
  columnBinning = {};
  bininngDataOptions: {} = {};
  binning = {
    names: new Set(),
    allBinnedColumns: new Set()
  };
  copyofbininngDataOptions: {};
  binningDatatoServer = {};
  selectedBinningValues: {} = {};
  newBinName = '';
  chkbinning: boolean;
  modalRefForManual: BsModalRef | null;
  config = {
    ignoreBackdropClick: true,
    class: 'preprocess-addfeature-manualmodal'
  };
  colnames: string[] = [];
  columnlistWithOutText = [];
  @Input() readOnly;

  Available = {
    'Skewness': false,
    'Outlier': false,
    'binning': false,
    'RemoveColumn': false
  };

  RecommendationMessage = {
    // 'Outlier': 'Observed 4% outlier in the attribute. Please replace the outlier(s) with the appropriate option',
    'Skewness': 'Observed skewness in the attribute. Please choose the appropriate method to remove skewness for optimal predictions',
    'RemoveColumn': 'Attribute contains only 2 unique values and is imbalanced. Please drop the attribute for optimal predictions',
    'binning': 'Observed unequal distribution of unique values. To achieve optimal prediction, please group minority unique values by using Binning technique'
  }

  OutlierMessage = {};

  constructor(private cus: CoreUtilsService, private ns: NotificationService, @Inject(ElementRef) private eleRef: ElementRef,
    private _modalService: BsModalService, private uts: UsageTrackingService) { }

  ngOnInit() {
    this.notcheckbinning = true;
    this.initDataModification();
  }

  initDataModification() {

    this.dataModificationFeatures = this.dataModification['Features'];
    this.autoBinning = this.dataModification['AutoBinning'];
    this.autoBinningDisableColumns = this.AutoBinningDisable;
    this.categoricalcolumns = this.NumericalAutoBinning;
    // this.featuresAddedbyUser = this.dataModification['NewAddFeatures']; // Prepopulated Add Feature  [Bug 709222, 708531, 708241 ]
    // this.dataToServer.DataModification['NewAddFeatures'] =
    //  (this.dataModification['NewAddFeatures'] === '' ) ? {} : this.dataModification['NewAddFeatures'] ; // Clonning Object
    let temp = '';
    if (this.dataModificationFeatures.hasOwnProperty('Interpolation')) {
      temp = this.dataModificationFeatures['Interpolation'];
      if (!this.cus.isNil(temp) && typeof temp === 'string') {
        temp = temp.trim();
      }
    }
    if (!this.cus.isNil(temp) && typeof temp === 'object') {
      temp = 'Linear';
    }

    this.interpolationDefaultValue = temp || 'Linear';
    if (this.problemTypeFlag) {
      this.dataToServer.DataModification['Interpolation'] = 'Linear';
    }

    for (const i in this.dataModificationFeatures) {
      if (i !== 'Interpolation') {
        const index = Object.keys(this.dataModificationFeatures).findIndex(item => item === i);
        this.dataModificationAttributes.push(i);
        if (this.dataModificationFeatures[i].hasOwnProperty('Outlier') && Object.keys(this.dataModificationFeatures[i]['Outlier']).length > 0) {
          this.dataModificationOutlierOptions[i] = Object.keys(this.dataModificationFeatures[i]['Outlier'] || {});

          this.OutlierMessage[i] = this.dataModificationFeatures[i]['Outlier']['Text'];
          const matchedIndex = this.dataModificationOutlierOptions[i].findIndex(d => d === 'None');
          if (matchedIndex < 0 && this.dataModificationOutlierOptions[i].length > 0) {
            this.dataModificationOutlierOptions[i].push('None');
          }

          this.defaultModificationOptions[i] = this.getKeyByValue(this.dataModificationFeatures[i]['Outlier'], 'True');

          // Bug Fix for 989919
          this.dataModificationOutlierCustomValue[i] = (this.dataModificationFeatures[i]['Outlier'] && (this.defaultModificationOptions[i] === undefined)) ?
            this.dataModificationFeatures[i]['Outlier']['CustomValue'] : '';

          if (this.dataModificationOutlierCustomValue[i] !== '' && this.defaultModificationOptions[i] === undefined) {
            this.radioTextBox['displayRadioTextBox' + index] = true;
          }
        }

        if (this.dataModificationFeatures[i].hasOwnProperty('Skewness') && Object.keys(this.dataModificationFeatures[i]['Skewness']).length > 0) {
          this.dataModificationSkewnessOptions[i] = Object.keys(this.dataModificationFeatures[i]['Skewness'] || {});
          if (this.dataModificationSkewnessOptions[i].length > 0) {
            const matchedIndex = this.dataModificationSkewnessOptions[i].findIndex(d => d === 'None');
            if (matchedIndex < 0 && this.dataModificationSkewnessOptions[i].length > 0) {
              this.dataModificationSkewnessOptions[i].push('None');
            }
          }
          if (this.dataModificationFeatures[i]['Skewness'] !== undefined) {
            this.defaultModificationOptionsSkewness[i] = this.getKeyByValue(this.dataModificationFeatures[i]['Skewness'], 'True');
          }
          this.dataModificationSkewnessCustomValue[i] = this.dataModificationFeatures[i]['Skewness'] ?
            this.dataModificationFeatures[i]['Skewness']['CustomValue'] : '';
        }

        if (this.dataModificationFeatures[i].hasOwnProperty('RemoveColumn') && Object.keys(this.dataModificationFeatures[i]['RemoveColumn']).length > 0) {
          this.dataModificationRemoveColumnOptions[i] = Object.keys(this.dataModificationFeatures[i]['RemoveColumn'] || {});
          if (this.dataModificationRemoveColumnOptions[i].length > 0) {
            const matchedIndex = this.dataModificationRemoveColumnOptions[i].findIndex(d => d === 'None');
            if (matchedIndex < 0 && this.dataModificationRemoveColumnOptions[i].length > 0) {
              this.dataModificationRemoveColumnOptions[i].push('None');
            }
          }
        }

      }
    }
    // (this.dataModificationOutlierOptions)

    // dataModification Binning
    this.columnBinning = this.dataModification['ColumnBinning'];
    for (const i in this.columnBinning) {

      if (this.columnBinning.hasOwnProperty(i)) {
        this.dataModificationAttributes.push(i);
        this.bininngDataOptions[i] = [];
        this.binning[i] = {};
        const attributeInfo = this.columnBinning[i];


        for (const subCat in attributeInfo) {
          if (attributeInfo.hasOwnProperty(subCat)) {
            const isNull = this.cus.isNil(this.columnBinning[i][subCat]['SubCatName']);
            let dropDownOption;
            if (!isNull) {
              dropDownOption = this.columnBinning[i][subCat]['SubCatName'] + '(' + this.columnBinning[i][subCat]['Value'] + '%)';
            }
            const binningName = this.columnBinning[i][subCat].NewName;
            this.binning.names.add(binningName);
            let mappedBinningData = {};

            if (attributeInfo[subCat]['Binning'] === 'True') {
              this.binning[i]['BinningNames'] = this.binning[i]['BinningNames'] || new Set([]);
              this.binning[i][binningName] = this.binning[i][binningName] || [];

              this.binning[i]['BinningNames'].add(binningName);

              if (!isNull) {
                this.binning.allBinnedColumns.add(dropDownOption);
                this.binning[i][binningName].push(dropDownOption);
              }

              // this.binning[binningName].push(dropDownOption)
            } else {
              if (!isNull) {
                this.bininngDataOptions[i].push(dropDownOption);
              }
            }
            if (subCat.indexOf('SubCat') > -1) {
              mappedBinningData = {
                'Attribute': i,
                'SubCatIndex': subCat,
                'SubCatName': attributeInfo[subCat]['SubCatName'],
                'Value': attributeInfo[subCat]['Value'],
                'Binning': attributeInfo[subCat]['Binning'],
                'NewName': attributeInfo[subCat]['NewName']
              };
              this.allColumnMappingArray.push(mappedBinningData);
            }
          }

        }
        this.copyofbininngDataOptions = JSON.parse(JSON.stringify(this.bininngDataOptions));
      }

    }

    // (this.dataModification)
    // (this.binning)
    if (this.binning['Type']) {
      // (true)
    }
    // priscription

    if (this.autoBinning !== undefined) {
      for (const i in this.autoBinning) {
        if (this.autoBinning.hasOwnProperty(i)) {
          this.autobinningcolumns.push(i);
        }
      }
    }

    // if (JSON.stringify(this.prepopulatecolums) !== '{}') {
    if (JSON.stringify(this.autoBinningDisableColumns) !== '{}') {
      for (const i in this.autoBinningDisableColumns) {
        if (this.autoBinningDisableColumns.hasOwnProperty(i)) {
          this.colnames.push(i);
          this.payloadautobinningcolumsitem[i] = 'True';
          if (this.categoricalcolumns.hasOwnProperty(i) === false) {
            this.autobinningcolumsitem[i] = 'True';
          }
          // this.setautobinningcolumsprepopulate.emit(this.autobinningcolumsitem);
          this.chkbinning = true;
          this.notcheckbinning = false;
        }
      }
    } else {
      this.chkbinning = false;
      this.notcheckbinning = true;
    }


    this.Available.Outlier = Object.keys(this.dataModificationOutlierOptions).length > 0;
    this.Available.Skewness = Object.keys(this.dataModificationSkewnessOptions).length > 0;
    this.Available.RemoveColumn = Object.keys(this.dataModificationRemoveColumnOptions).length > 0;
    if(this.columnBinning !== undefined){
      this.Available.binning = Object.keys(this.columnBinning).length > 0;
    }  
    this.setDatatoserverDataModification.emit(this.dataToServer.DataModification);
  }

  bindbinningcolums(colsname: any) {
    this.autobinningcolumsitem = {};
    this.payloadautobinningcolumsitem = {};
    const items = colsname;
    items.forEach(item => {
      this.payloadautobinningcolumsitem[item] = 'True';
      if (this.categoricalcolumns.hasOwnProperty(item) === false) {
        this.autobinningcolumsitem[item] = 'True';
      }
      const deletedBinlist = this.allColumnMappingArray.filter(colmapp => colmapp.Attribute === item && colmapp.Binning === 'True');
      for (const filterBinnings in deletedBinlist) {
        if (deletedBinlist.hasOwnProperty(filterBinnings) && !this.cus.isNil(filterBinnings)) {
          const filterBinning = deletedBinlist[filterBinnings];
          this.binningDatatoServer[item] = this.binningDatatoServer[item] || {};
          this.binningDatatoServer[item][filterBinning.SubCatIndex] = {
            'SubCatName': filterBinning.SubCatName + '(' + filterBinning.Value + '%)',
            'Binning': 'False',
            'NewName': 'False',
          };
        }
      }
    });

    this.dataToServer['DataModification'].binning = this.binningDatatoServer;
    this.setDatatoserverDataModification.emit(this.dataToServer.DataModification);
    this.setautobinningcolums.emit(this.payloadautobinningcolumsitem);
  }

  onChecked(elementRef) {
    if (elementRef.checked === true) {
      this.notcheckbinning = false;
    } else {
      this.colnames = [];
      this.autobinningcolumsitem = {};
      this.payloadautobinningcolumsitem = {};
      this.setautobinningcolums.emit(this.payloadautobinningcolumsitem);
      this.notcheckbinning = true;
    }
  }

  getToolTipData(data) {
    if (data && data.length) {
      let colname = '';
      data.forEach(res => {
        colname += res + ' ';
      });
      return colname;
    }
  }


  onRadioClick(event, attribute, i, type) {
    const index = i;

    if (event.target.tagName === 'INPUT' && event.target.type === 'radio') {
      if (event.target.value === 'CustomValue') {
        this.radioTextBox['displayRadioTextBox' + index] = true;
        if (type === 'Outlier') {
          this.onChange(i,'Outlier', attribute, 'CustomValue', this.dataModificationOutlierCustomValue[attribute]);
        }
        if (type === 'Skewness') {
          this.onChange(i,'Skewness', attribute, 'CustomValue', this.dataModificationOutlierCustomValue[attribute]);
        }
        if (type === 'RemoveColumn') {
          this.onChange(i,'RemoveColumn', attribute, 'CustomValue', this.dataModificationOutlierCustomValue[attribute]);
        }
      }
      // Bug Fix for 989919
      // else {
      //   this.radioTextBox['displayRadioTextBox' + index] = false;
      // }
      else if (type === 'Outlier') {
        this.radioTextBox['displayRadioTextBox' + index] = false;
        this.dataModificationOutlierCustomValue[attribute] = "";
      }
    }

  }

  onChange(i, tabName, attribute, selectedOption, inputValue) {
    // (tabName, attribute, selectedOption, inputValue)

    if (selectedOption === 'CustomValue') {
      this.radioTextBox['displayRadioTextBox' + i] = true;
      if (inputValue !== null && inputValue !== undefined) {
        const valid = this.cus.isSpecialCharacter(inputValue);
        if (valid === 0) {
          return 0;
        }
   
        const params = [tabName, attribute, selectedOption, inputValue];
        this.setDatatoserverDataModificationOnChange.emit(params);
      } else {
        if ( inputValue) {
          this.ns.warning('Enter custom value');
        }
      }
    } else {
      const params = [tabName, attribute, selectedOption, null];
      this.setDatatoserverDataModificationOnChange.emit(params);
    }
  }

  getKeyByValue(object, value) {
    if (!object) {
      return '';
    }

    delete object.ChangeRequest;
    delete object.PChangeRequest;
    const result = Object.keys(object).filter(key => object[key] === value);
    return result[0];
  }



  removeBinning(attribute, name, index) {
    const binningNames = this.binning[attribute]['BinningNames'];
    this.binning[attribute][name].forEach(column => {
      this.bininngDataOptions[attribute].unshift(column);
    });

    delete this.binning[attribute][name];
    // delete this.binningDatatoServer[attribute]['SubCat' + index];
    const deletedBinlist = this.allColumnMappingArray.filter(colmapp => colmapp.Attribute === attribute
      && colmapp.NewName === name && colmapp.Binning === 'True');
    for (const filterBinnings in deletedBinlist) {
      if (deletedBinlist.hasOwnProperty(filterBinnings) && !this.cus.isNil(filterBinnings)) {
        const filterBinning = deletedBinlist[filterBinnings];
        this.binningDatatoServer[attribute] = this.binningDatatoServer[attribute] || {};
        this.binningDatatoServer[attribute][filterBinning.SubCatIndex] = {
          'SubCatName': filterBinning.SubCatName + '(' + filterBinning.Value + '%)',
          'Binning': 'False',
          'NewName': 'False',
        };
      }
    }
    this.deleteBinsExistInDataToServer(attribute, name);
    this.dataToServer['DataModification'].binning = this.binningDatatoServer;
    binningNames.delete(name);
    this.setDatatoserverDataModification.emit(this.dataToServer.DataModification);
  }

  deleteBinsExistInDataToServer(attribute, name) {
    for (const bindata in this.binningDatatoServer[attribute]) {
      if (this.binningDatatoServer[attribute].hasOwnProperty(bindata)) {
        const binningdata = this.binningDatatoServer[attribute][bindata];
        if (!this.cus.isNil(binningdata) && !this.cus.isNil(binningdata.NewName) && binningdata.NewName === name) {
          delete this.binningDatatoServer[attribute][bindata];
        }
      }
    }
  }

  removeWhiteSpace(attributeName) {
    if (!this.cus.isNil(attributeName)) {
      return attributeName.replace(/[\s]/g, '');
    }
    return attributeName;
  }

  createNewBin(attribute, binningInput, binningInputElement) {

    binningInput = binningInput ? binningInput.trim() : '';
    if (binningInput === '') {
      this.ns.warning('Please enter Binning Name');
    } else if (this.cus.isNil(this.selectedBinningValues[attribute]) || this.selectedBinningValues[attribute].length === 0) {
      this.ns.warning('Please select the values to be Binned for the Recommended Attribute under Data Modification section');
    } else {
      this.newBinName = binningInput;
      this.binning[attribute] = this.binning[attribute] || {};
      const existingBinnings = this.binning[attribute];

      this.addExisitngBiningDatatoServer(existingBinnings, attribute);

      this.binning[attribute][this.newBinName] = this.binning[attribute][this.newBinName] || [];
      this.binning[attribute][this.newBinName] = this.binning[attribute][this.newBinName].concat(this.selectedBinningValues[attribute]);
      this.binning.names.add(this.newBinName);
      this.binning[attribute]['BinningNames'] = this.binning[attribute]['BinningNames'] || new Set([]);
      this.binning[attribute]['BinningNames'].add(this.newBinName);

      this.addNewBinningDatatoServer(attribute, binningInput);

      this.selectedBinningValues[attribute].forEach(column => {
        const index = this.bininngDataOptions[attribute].indexOf(column);
        this.bininngDataOptions[attribute].splice(index, 1);
      });

      this.bininngDataOptions = JSON.parse(JSON.stringify(this.bininngDataOptions));

      this.dataToServer['DataModification'].binning = this.binningDatatoServer;
      this.newBinName = '';
      this.selectedBinningValues[attribute] = [];
      binningInputElement.value = '';
      // const multiDropDownElemnet = this.eleRef.nativeElement;
      // const multiDropdownEle = !this.cus.isNil(multiDropDownElemnet) ?
      //   multiDropDownElemnet.querySelector('#' + this.removeWhiteSpace(attribute)) : null;
      // if (!this.cus.isNil(multiDropdownEle)) {
      //   multiDropdownEle.querySelector('.multiselect-dropdown-icon').click();
      // }
    }
    this.setDatatoserverDataModification.emit(this.dataToServer.DataModification);
  }


  addExisitngBiningDatatoServer(existingBinnings: any, attribute: string) {
    for (const bindcollection in existingBinnings) {
      if (existingBinnings.hasOwnProperty(bindcollection) && bindcollection !== 'BinningNames') {
        const existBinValues = existingBinnings[bindcollection];
        for (const binValue in existBinValues) {
          if (existBinValues.hasOwnProperty(binValue)) {
            const splirArrBins = !this.cus.isNil(existBinValues[binValue])
              && existBinValues[binValue].indexOf('(') > -1 ? existBinValues[binValue].split('(') : null;
            const subCatName = !this.cus.isNil(splirArrBins) && splirArrBins.length !== 0 ? splirArrBins[0] : null;
            const splitArrBinValues = !this.cus.isNil(splirArrBins) && splirArrBins.length !== 0 ? splirArrBins[1].split('%)') : null;
            const value = !this.cus.isNil(splitArrBinValues) && splitArrBinValues.length !== 0 ? splitArrBinValues[0] : null;

            const filterBinnings = this.allColumnMappingArray.find(colmapp =>
              colmapp.Attribute === attribute && colmapp.SubCatName === subCatName && colmapp.Value === value
              && colmapp.Binning === 'True');
            if (!this.cus.isNil(filterBinnings)) {
              this.binningDatatoServer[attribute] = this.binningDatatoServer[attribute] || {};
              const validateBinningData = this.binningDatatoServer[attribute][filterBinnings.SubCatIndex];
              if (!this.cus.isNil(validateBinningData) && validateBinningData.Binning === 'False'
                && validateBinningData.NewName === 'False') {
                this.binningDatatoServer[attribute][filterBinnings.SubCatIndex] = {
                  'SubCatName': filterBinnings.SubCatName + '(' + filterBinnings.Value + '%)',
                  'Binning': filterBinnings.Binning,
                  'NewName': filterBinnings.NewName
                };
              } else if (this.cus.isNil(validateBinningData)) {
                this.binningDatatoServer[attribute][filterBinnings.SubCatIndex] = {
                  'SubCatName': filterBinnings.SubCatName + '(' + filterBinnings.Value + '%)',
                  'Binning': filterBinnings.Binning,
                  'NewName': filterBinnings.NewName
                };
              }
            }
          }
        }
      }
    }
  }
  addNewBinningDatatoServer(attribute: string, binningInputName: string) {
    let splitArrBinValues;
    let splirArrBins;
    let subCatName;
    this.selectedBinningValues[attribute].forEach(column => {
      if (!this.cus.isNil(column)) {
        splirArrBins = !this.cus.isNil(column) && column.indexOf('(') > -1 ? column.split('(') : null;
        if (splirArrBins[1].indexOf('%)') > -1) {
          subCatName = !this.cus.isNil(splirArrBins) && splirArrBins.length !== 0 ? splirArrBins[0] : null;
        } else {
          subCatName = splirArrBins[0] + '(' + splirArrBins[1];
        }

        if (splirArrBins[1].indexOf('%)') > -1) {
          splitArrBinValues = !this.cus.isNil(splirArrBins) && splirArrBins.length !== 0 ? splirArrBins[1].split('%)') : null;
        } else {
          const splirArrBinsval = !this.cus.isNil(column) && column.indexOf('(') > -1 ? column.split('(') : null;
          splitArrBinValues = !this.cus.isNil(splirArrBinsval) && splirArrBinsval.length !== 0 ? splirArrBinsval[2].split('%)') : null;
        }
      }
      const value = !this.cus.isNil(splitArrBinValues) && splitArrBinValues.length !== 0 ? splitArrBinValues[0] : null;
      const filterBinnings = this.allColumnMappingArray.find(colmapp =>
        colmapp.Attribute === attribute && colmapp.SubCatName === subCatName && colmapp.Value === value
      );
      this.binningDatatoServer[attribute] = this.binningDatatoServer[attribute] || {};
      this.binningDatatoServer[attribute][filterBinnings.SubCatIndex] = {
        'SubCatName': column,
        'Binning': 'True',
        'NewName': binningInputName
      };
    });
  }


  onBinningDataChange(attribute, selectedOptions) {
    this.selectedBinningValues[attribute] = selectedOptions;
  }



  openAddFeatureTypeSelection(addFeatureManualModal: TemplateRef<any>) {
    if (this.modelType !== 'TimeSeries') {
      this.uts.usageTracking('Data Engineering', 'Add Feature');
      // this.modalRefForType = this._modalService.show(addFeatureSelectionPopUP, { class: 'preprocess-addfeature' });
      this.modalRefForManual = this._modalService.show(addFeatureManualModal, this.config);
    }
  }
}
