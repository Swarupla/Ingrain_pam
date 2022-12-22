import { Component, OnInit, Input, ElementRef, OnChanges } from '@angular/core';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { FormBuilder, FormGroup, FormArray, AbstractControl, FormControl } from '@angular/forms';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { Options } from 'ng5-slider';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';

class IColumnData {
  Lemmitize: string;
  Stemming: string;
  Pos: string;
  Stopwords: Array<string>;
  Most_Frequent: number;
  Least_Frequent: number;
  // nGramSelector: Array<Number>;
}
@Component({
  selector: 'app-txt-preprocess',
  templateUrl: './txt-preprocess.component.html',
  styleUrls: ['./txt-preprocess.component.scss'],
  host: {'(document:click)': 'onClick($event)'}
})
export class TxtPreprocessComponent implements OnInit, OnChanges {

  @Input() columnList: any;
  @Input() savedColumnsAttributesData: any;
  @Input() silhouette_graph: string;
  textPreprocessedData: FormGroup;
  columnAttributesData = {} as IColumnData;
  populateSavedColumns = {}; // After apply saved data in populateSavedColumns

  // nGramSelectorSet = [1, 2, 3, 4, 5];
  // nGramSelectorSecondSet = [3, 4, 5];
  featureGeneration = ['Count Vectorizer', 'Tfidf Vectorizer', 'Word2Vec', 'Glove'];
  txtColumn = [];

  textNormalizationDropDownValuesPayload;
  // Lemmatization, Stemming and Part of Speech (POS)
  textNormalizationDropDownValues = ['Lemmatization', 'Stemming'];
  editNormalizationValues = ['Lemmatization'];

  customStopWords = [];
  stopWords = '';
  textNormalization: any;
  textColumnChosen = '';
  mostFrequentWords: any;
  leastFrequentWords: any;
  cluster: any = 1;
  clusterflag: boolean;

  DeletedTextColumnByUser = {};


  ngramOne: Number = 3;
  ngramTwo: Number = 3;
  options: Options = {
    floor: 1,
    ceil: 5,
    step: 1,
    noSwitching: true
  };

  showSilhouette: Boolean;

  aggregationList: any = ["Sum", "Mean", "Min", "Max", "Min-Max Concat", "TF-IDF Weightage"];
  clusterToggle: boolean;
  isAggregationVisible: boolean = false;
  @Input() readOnly;

  constructor(private _notificationService: NotificationService, private des: DataEngineeringService,
    private _eref: ElementRef,
    private coreUtilService: CoreUtilsService) {
    this.textPreprocessedData = new FormGroup({
      // customStopWords: new FormControl(''),
      removeMostFrequentWords: new FormControl(0),
      removeLeastFrequentWords: new FormControl(0),
      cluster: new FormControl(1),
      // nGramSelector: new FormControl(3),
      // nGramSelectorSecond: new FormControl(3),
      // normalization: new FormControl(''),
      featureGeneration: new FormControl(''),
      aggregation: new FormControl(''),
      clustering: new FormControl(''),
    });
  }

  ngOnInit() {
  }

  ngOnChanges() {
    this.clusterToggle = true;
    this.clusterflag = true;
    this.txtColumn = Object.assign([], this.columnList);
    this.textColumnChosen = '';
    this.txtColumn.forEach(element => {
      this.DeletedTextColumnByUser[element] = 'true';
    });
    this.des.deleteByUser = this.DeletedTextColumnByUser;
    this.columnAttributesData.Lemmitize = 'False';
    this.columnAttributesData.Stemming = 'False';
    // this.columnAttributesData.Pos = 'False';
    this.prePopulateSavedColumns(this.savedColumnsAttributesData);
  }


  setTextColumn(columnName) {
    this.textColumnChosen = columnName;
    this.resetValues();
  }

  onChangeNormalization(data) {
    // console.log(data);
    this.textNormalizationDropDownValuesPayload = { 'Lemmitize': 'False', 'Stemming': 'False', 'Pos': 'False' };
    const d: Array<string> = data;
    d.forEach((element) => {
      if (element === 'Lemmatization') {
        this.textNormalizationDropDownValuesPayload['Lemmitize'] = 'True';
      }
      if (element === 'Stemming') {
        this.textNormalizationDropDownValuesPayload['Stemming'] = 'True';
      }
      if (element === 'Part of Speech (POS)') {
        this.textNormalizationDropDownValuesPayload['Pos'] = 'True';
      }
    });
    // console.log(this.textNormalizationDropDownValuesPayload);
    this.columnAttributesData.Lemmitize = this.textNormalizationDropDownValuesPayload.Lemmitize;
    this.columnAttributesData.Stemming = this.textNormalizationDropDownValuesPayload.Stemming;
    // this.columnAttributesData.Pos = this.textNormalizationDropDownValuesPayload.Pos;

  }


  // Save changes
  saveColumnTextAttributes(columnName: string, saveALL?: string) {

    const formControl = this.textPreprocessedData.controls;
    if (!(this.textColumnChosen && this.textColumnChosen.length > 0)) {
      this._notificationService.warning('Please select column');
    } else if (formControl.featureGeneration.value === '') {
      this._notificationService.warning('Please fill all mandatory fields');
    } else if (this.clusterToggle == false && 
      (formControl.featureGeneration.value == 'Word2Vec' || formControl.featureGeneration.value == 'Glove') &&
      formControl.aggregation.value == '') {
      this._notificationService.warning('Please fill all mandatory fields'); 
      } else {
      this.DeletedTextColumnByUser[columnName] = 'false';
      this.des.deleteByUser = this.DeletedTextColumnByUser;
      const key1 = columnName;
      this.columnAttributesData.Stopwords = Object.assign([], this.customStopWords);
      this.columnAttributesData.Least_Frequent = formControl.removeLeastFrequentWords.value * 1;
      this.columnAttributesData.Most_Frequent = formControl.removeMostFrequentWords.value * 1;

      this.populateSavedColumns[key1] = Object.assign({}, this.columnAttributesData);
      this.populateSavedColumns['Feature_Generator'] = formControl.featureGeneration.value;
      this.populateSavedColumns['N-grams'] = [this.ngramOne, this.ngramTwo];
      this.populateSavedColumns['NumberOfCluster'] = formControl.cluster.value * 1;
      if (this.clusterflag === true) {
        this.populateSavedColumns['Clustering'] = 'True';
      } else {
      this.populateSavedColumns['Clustering'] = formControl.clustering.value === true ? 'True' : 'False'; }
      this.populateSavedColumns['Aggregation'] = formControl.aggregation.value;
      // if (!formControl.clustering.value &&
      //   (formControl.featureGeneration.value == 'Word2Vec' ||
      //     formControl.featureGeneration.value == 'Glove')) {
      //   this.populateSavedColumns['Aggregation'] = formControl.aggregation.value;
      // } else {
      //   formControl.aggregation.setValue('');
      //   this.populateSavedColumns['Aggregation'] = formControl.aggregation.value;
      // }
      if (saveALL === 'ONE') {
        this.resetValues();
        this._notificationService.success('Column saved temporarily, Please Click on Save/Apply to update the changes');
      }
      if (saveALL === 'ALL') {
        if (columnName === this.columnList[this.columnList.length - 1]) {
          this._notificationService.success('Columns saved temporarily,Please Click on Save/Apply to update the changes');
        }
      }
      // const index = this.txtColumn.indexOf(key1, 0);
      // if (index > -1) {
      //   this.txtColumn.splice(index, 1);
      // }
      this.textColumnChosen = '';

      // storing value in service
      this.des.saveTextDataPreprocessing(this.populateSavedColumns);
      console.log('Data send to API', this.populateSavedColumns);
    }
  }

  // Save ALL Columns
  saveAllColumns() {
    this.columnList.forEach(element => {
      // console.log(element);
      this.textColumnChosen = element;
      this.saveColumnTextAttributes(element, 'ALL');
    });
    // this._notificationService.success('Columns saved successfully');
  }

  // Prepopulate data
  prePopulateSavedColumns(data) {
    // console.log(data);
    if ( data.hasOwnProperty('TextColumnsDeletedByUser')) {
      delete data.TextColumnsDeletedByUser;
    }
    const columnNames = Object.keys(data);

    columnNames.forEach(element => {
      this.textPreprocessedData.controls.featureGeneration.setValue(data['Feature_Generator']);
      this.textPreprocessedData.controls.cluster.setValue(data['NumberOfCluster']);
      this.textPreprocessedData.controls.aggregation.setValue(data['Aggregation']);
      this.textPreprocessedData.controls.clustering.setValue(data['Clustering']);
      this.clusterToggle = data['Clustering'] === 'True' ? true : false;
      this.cluster = data['NumberOfCluster'];

      if (!this.clusterToggle &&
        (this.textPreprocessedData.controls.featureGeneration.value == 'Word2Vec' ||
          this.textPreprocessedData.controls.featureGeneration.value == 'Glove')) {
        this.isAggregationVisible = true;
      } else {
        this.isAggregationVisible = false;
      }

      // this.setSecondNGramValue(data['N-grams'][0] * 1);
      this.ngramOne = data['N-grams'][0] * 1;
      this.ngramTwo = data['N-grams'][1] * 1;
      // this.textPreprocessedData.controls.nGramSelector.setValue(data['N-grams'][0] * 1);
      // this.textPreprocessedData.controls.nGramSelectorSecond.setValue(data['N-grams'][1] * 1);
      if (this.txtColumn.indexOf(element) > -1) {
        this.columnAttributesData.Lemmitize = data[element].Lemmitize;
        this.columnAttributesData.Stemming = data[element].Stemming;
        // this.columnAttributesData.Pos = data[element].Pos;
        this.editSavedColumns(element, data[element]);
        this.saveColumnTextAttributes(element);
      }

      // const index = this.txtColumn.indexOf(element, 0);
      // if (index > -1) {
      //   this.txtColumn.splice(index, 1);
      // }
    });

  }

  // Edit Populate Saved columns
  editSavedColumns(columnTitle, data?) {
    if (data) { this.populateSavedColumns[columnTitle] = Object.assign({}, data); }
    const details: IColumnData = Object.assign({}, this.populateSavedColumns[columnTitle]);

    this.textColumnChosen = columnTitle;
    // console.log(details);
    const formControl = this.textPreprocessedData.controls;
    
    if ( Object.keys(details).length > 0 ) {
    formControl.removeLeastFrequentWords.setValue(details.Least_Frequent);
    formControl.removeMostFrequentWords.setValue(details.Most_Frequent);
    this.leastFrequentWords = details.Least_Frequent;
    this.mostFrequentWords = details.Most_Frequent;

    if (details.Stopwords.length > 0) {
      this.customStopWords = details.Stopwords;
    } else {
      this.customStopWords = [];
    }

    const populatesavedData = [];
    if (details.Lemmitize === 'True') {
      populatesavedData.push(this.textNormalizationDropDownValues[0]);
    }
    if (details.Stemming === 'True') {
      populatesavedData.push(this.textNormalizationDropDownValues[1]);
    }
    // if (details.Pos === 'True') {
    //   populatesavedData.push(this.textNormalizationDropDownValues[2]);
    // }
    // });
    this.editNormalizationValues = populatesavedData;
   }
  }

  resetValues() {
    // this.textPreprocessedData.reset();
    const formControl = this.textPreprocessedData.controls;
    formControl.removeLeastFrequentWords.setValue(0);
    formControl.removeMostFrequentWords.setValue(0);
    // formControl.featureGeneration.setValue('');
    this.editNormalizationValues = ['Lemmatization'];
    this.customStopWords = [];
    this.mostFrequentWords = 0;
    this.leastFrequentWords = 0;
  }

  // Delete data
  deleteColumnDetails(columnName) {
    delete this.populateSavedColumns[columnName];
    // this.txtColumn.push(columnName);
    this.DeletedTextColumnByUser[columnName] = 'true';
    this.textColumnChosen = '';
    this.des.deleteByUser = this.DeletedTextColumnByUser;
    this.resetValues();
  }


  // setSecondNGramValue(value: number) {
  // const index = this.nGramSelectorSet.indexOf(value * 1);
  // this.nGramSelectorSecondSet = [];
  // for ( let i = index; i < this.nGramSelectorSet.length; i++) {
  // this.nGramSelectorSecondSet.push(this.nGramSelectorSet[i]);
  // this.textPreprocessedData.controls.nGramSelectorSecond.setValue(this.nGramSelectorSecondSet[0] * 1);
  // }

  // }


  changedValueForMostFrequentWords(value) {
    this.mostFrequentWords = value * 1;
  }

  changedValueForLeastFrequentWords(value) {
    this.leastFrequentWords = value * 1;
  }

  changedValueForNumberOfCluster(value) {
    this.cluster = value * 1;
  }

  // Add words
  addWords() {
    if (this.stopWords === '') {

    } else {
      // Start Input Validation - DIYA Scanning
      const valid = this.coreUtilService.isSpecialCharacter(this.stopWords);
      if ( valid === 0) {
       return 0;
      } else {
        this.customStopWords.push(this.stopWords);
        this.stopWords = '';
      }
      // End Input Validation - DIYA Scanning
    }
  }

  validateAddWords() {
    if (this.stopWords === '') {
      return 'btn-secondary';
    } else {
      return 'btn-primary';
    }
  }

  // Remove words

  removeWords(index) {
    this.customStopWords.splice(index, 1);
  }

  getStyles(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 10;
    left = ((rangefromMinValue / diff) * 100);
    if ( left < 10) {
      left = 10;
    }
    if (left > 78) {
      left = left - 5;
    }
    if (number === maxValue) {
      left = 88;
    }

    const styles = {
      'position': 'absolute',
      'left': left + '%',
      'z-index': '1',
      'top': '4em',
      'color': 'black',
      'font-size': '12px',
      'font-weight': '700'
    };
    return styles;
  }

  getStyleCluster(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 10;
    left = ((rangefromMinValue / diff) * 100);
    if ( left < 10) {
      left = 10;
    }
    if (left > 78) {
      left = left - 5;
    }
    if (number === maxValue) {
      left = 88;
    }

    const styles = {
      'position': 'absolute',
      'left': left + '%',
      'z-index': '1',
      'top': '6.5em',
      'color': 'black',
      'font-size': '12px',
      'font-weight': '700'
    };
    return styles;
  }


  onClick(event) {
    if (this._eref.nativeElement.contains(event.target) && event.target.title === 'View Silhouette score graph'
      && event.target.className === 'cursor-pointer'
      && this.silhouette_graph && this.silhouette_graph.length > 0) {
      this.showSilhouette = true;
    } else if (event.target.title !== 'View Silhouette score graph') {
      this.showSilhouette = false;
    }
  }

  onClusterSwitchChanged(elementRef) {
     this.clusterflag = false;
    if (elementRef.checked === true) {
        this.clusterToggle = true;
      if (this.textPreprocessedData.controls.featureGeneration.value == 'Word2Vec' ||
        this.textPreprocessedData.controls.featureGeneration.value == 'Glove') {
        this.isAggregationVisible = false;
        this.textPreprocessedData.controls.aggregation.setValue('');
      }
    } else if (elementRef.checked === false) {
      this.clusterToggle = false;
      if (this.textPreprocessedData.controls.featureGeneration.value == 'Word2Vec' ||
        this.textPreprocessedData.controls.featureGeneration.value == 'Glove') {
        this.isAggregationVisible = true;
      }
    }
  }

  onFeatureGenerationChange(featureGeneration) {
    if ((this.textPreprocessedData.controls.featureGeneration.value == 'Word2Vec' ||
      this.textPreprocessedData.controls.featureGeneration.value == 'Glove') &&
      this.clusterToggle == false) {
      this.isAggregationVisible = true;
    } else {
      this.isAggregationVisible = false;
      this.textPreprocessedData.controls.aggregation.setValue('');
    }
  }

  setAggregation(aggregationName) {
    // console.log('aggregationName: ' + aggregationName);
  }

  selectedTextColumn(txtColumnName) {
    if ( !this.populateSavedColumns.hasOwnProperty(txtColumnName) ) {
      this.textColumnChosen = txtColumnName;
    }
  }
}

