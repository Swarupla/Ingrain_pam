import { Component, OnInit, ElementRef, Inject, TemplateRef, ViewChild } from '@angular/core';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { throwError } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { unescapeIdentifier } from '@angular/compiler';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { tap } from 'rxjs/operators';

import { ValidRecordDetailsPopupComponent } from '../valid-record-details-popup/valid-record-details-popup.component';
import { Router } from '@angular/router';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
@Component({
  selector: 'app-pst-feature-mapping',
  templateUrl: './pst-feature-mapping.component.html',
  styleUrls: ['./pst-feature-mapping.component.scss']
})
export class PstFeatureMappingComponent implements OnInit {

  @ViewChild('confirmValidRecModels', { static: true }) confirmValidRecModels: TemplateRef<any>;


  correlationId: string;
  featuresColumnForMapping: {} = {};
  mappingOptionSelected: {} = {};
  existingColumnList = [];
  isExistingTemplate: boolean;
  isNewModel: boolean;
  disableIndex = 0;

  tempArray: Set<string>;
  tempIndexes: number[] = [];
  deletedValues = new Set([]);
  targetColumnForMap = '';
  isTargetColMapped = true;
  selectedIndex = 0;
  customcolumn: any = '';
  displayaddCustomColumn = false;
  featureListForReset;
  resetValue = false;
  defaultSelected;
  uniqueIdentifierVal = '';
  isUniqueIdentifierMap = false;
  selectedUniqueIndex = 0;
  countofExistingColList;
  ifTimeSeriesModel = false;

  timeSeriesDateColumn = '';
  isTimeSeresDateColumnMapped = false;
  selectedTimeDateColIndex = 0;
  UniquenessDetails: any;
  ifDateKey: boolean;
  timeSeriesDataType = {};
  ifLessRecords: boolean = false;
  validRecMessage: string;

  modalRef: BsModalRef;

  config = {
    ignoreBackdropClick: true,
    class: 'deploymodle-confirmpopup'
  };

  IfModelTemplateDataSource: boolean;

  constructor(private dialogRef: DialogRef,
    private dialogConfig: DialogConfig,
    private _problemStatementService: ProblemStatementService,
    private _notificationService: NotificationService,
    private _appUtilsService: AppUtilsService,
    private _coreUtilService: CoreUtilsService,
    private localStorageService: LocalStorageService,
    private _modalService: BsModalService, private router: Router,) { }

  ngOnInit() {
    this.correlationId = this.dialogConfig.data.correlationIdForMappingCol;
    this._appUtilsService.loadingStarted();
    this.loadFeaturesColumnList(this.dialogConfig.data.featureColumnList,
      this.dialogConfig.data.correlationIdForMappingCol,
      this.dialogConfig.data.existingTargetColumn, this.dialogConfig.data.existingUniqueIdentifier,
      this.dialogConfig.data.isTimeSeriesModel);
  }

  onClose() {
    this.dialogRef.close();
  }

  getSelection(mappingColumnList, exitingTempleteFeatureCol): boolean {
    return mappingColumnList.replace(/\s/g, '') === exitingTempleteFeatureCol.replace(/\s/g, '');
  }

  getColumnsData(exitingTempleteFeatureCol, index) {
    return this.featuresColumnForMapping[exitingTempleteFeatureCol];
  }

  loadFeaturesColumnList(featureColumnList, correlationIdForMappingCol, targetColumn, uniqueIdentifier, isTimeSeriesModel) {
    this.ifTimeSeriesModel = isTimeSeriesModel;
    this.uniqueIdentifierVal = uniqueIdentifier;
    let showMessage = false;
    this.targetColumnForMap = targetColumn;
    featureColumnList.forEach((feature) => { // BugFix 816956
      if (feature == '') {
        featureColumnList.delete(feature);
      }
    });
    this.existingColumnList = Array.from(featureColumnList);
    if (!this.ifTimeSeriesModel) {
      this.existingColumnList.unshift(targetColumn, uniqueIdentifier);
    } else if (this.ifTimeSeriesModel) {
      this.timeSeriesDateColumn = this.existingColumnList.toString();
      if (uniqueIdentifier !== '') {
        this.existingColumnList.unshift(targetColumn, this.timeSeriesDateColumn, uniqueIdentifier);
      } else {
        this.existingColumnList.unshift(targetColumn, this.timeSeriesDateColumn);
      }
      this.existingColumnList.pop();
    }
    this.isExistingTemplate = false;
    this.isNewModel = true;
    const availableColList = this.existingColumnList;
    this.countofExistingColList = (availableColList.length) - 1;

    this._problemStatementService.getColumns(correlationIdForMappingCol,
      this.isExistingTemplate, this.isNewModel).subscribe(featureColList => {
        if (featureColList.ColumnsList) {
          this.IfModelTemplateDataSource = featureColList.IsModelTemplateDataSource;

          sessionStorage.setItem('isModelDeployed', featureColList.IsModelDeployed)

          if (featureColList.ValidRecordsDetails !== undefined && featureColList.ValidRecordsDetails !== null) {
            this.validRecordDetailsPopup(featureColList.ValidRecordsDetails);
          }

          this.timeSeriesDataType = featureColList.DataTypeColumns;
          // const dateDatatype = Date;	
          // this.ifDateKey = this.getKeyByValue(featureColList.DataTypeColumns, 'Date');	
          if (featureColList.UniquenessDetails !== undefined && featureColList.UniquenessDetails !== null) {
            this.UniquenessDetails = featureColList.UniquenessDetails;
          }
          this._problemStatementService.storeData(featureColList);
          this.featureListForReset = featureColList.ColumnsList;
          const newColumnList = (featureColList.ColumnsList) ? (featureColList.ColumnsList) : '';

          this.existingColumnList.forEach((value) =>
            this.featuresColumnForMapping[value] = newColumnList ? new Set(newColumnList) : new Set([])
          );
          this.featuresColumnForMapping['temp'] = newColumnList ? new Set(newColumnList) : new Set([]);
          this.existingColumnList.forEach((value) => {
            if (newColumnList.indexOf(value) > -1) {
              this.onMappingOptionSelected(value, value);
            }


            const newColumnListsort = this.existingColumnList.filter(v => newColumnList.includes(v));

            for (let index = 0; index < this.existingColumnList.length; index++) {
              if (this.existingColumnList[index] === newColumnListsort[index]) {
                showMessage = true;
              }
            }
          });
          if (showMessage) {
            this._notificationService.warning('Please review the mapped features and change as required.');
          }
        }
        this._appUtilsService.loadingEnded();
      },
        error => {
          this._appUtilsService.loadingEnded();
          throwError(error); this._notificationService.error('Something went wrong.');
        }
      );
  }

  onMappingOptionSelected(key, value) {
    this.resetValue = false;
    this.mappingOptionSelected[key] = value;

    this.existingColumnList.forEach((val) => {
      const tempMappingOption = Object.assign({}, this.mappingOptionSelected);
      this.featuresColumnForMapping[val] = new Set(this.featuresColumnForMapping['temp']);
      delete tempMappingOption[val];
      Object.keys(tempMappingOption).map(item => tempMappingOption[item]).forEach(
        data => this.featuresColumnForMapping[val].delete(data)
      );
    });
    if (this.mappingOptionSelected.hasOwnProperty(this.targetColumnForMap)) {
      this.isTargetColMapped = true;
    } else {
      this.isTargetColMapped = false;
    }
    if (!this.ifTimeSeriesModel) {
      if (this.mappingOptionSelected.hasOwnProperty(this.uniqueIdentifierVal)) {
        this.isUniqueIdentifierMap = true;
      }
    } else if (this.ifTimeSeriesModel) {
      if (this.mappingOptionSelected.hasOwnProperty(this.timeSeriesDateColumn)) {
        if (key === this.timeSeriesDateColumn) {
          if (this.timeSeriesDataType[value] === 'Date') {
            this.isTimeSeresDateColumnMapped = true;
          } else {
            this.isTimeSeresDateColumnMapped = false;
            this.selectedTimeDateColIndex = 1;
          }
        }
      }
    }
  }

  applyColumnMapping() {
    this.resetValue = false;
    const iterator = this.existingColumnList;
    const objData = {
      'mappingObj': this.mappingOptionSelected,
      'unique': this.UniquenessDetails,
      'IfModelTemplateDataSource': this.IfModelTemplateDataSource
    };
    if (Object.entries(this.mappingOptionSelected).length !== 0) {
      if (!this.ifTimeSeriesModel) {
        if (this.mappingOptionSelected.hasOwnProperty(this.targetColumnForMap)
          && this.mappingOptionSelected.hasOwnProperty(this.uniqueIdentifierVal)) {
          // this.dialogRef.close(this.mappingOptionSelected);	
          this.dialogRef.close(objData);
          if (localStorage.getItem('oldCorrelationID') === 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6') {
            let modelName = this.localStorageService.modelName;
            let msg = 'Model training of first model, ' + modelName + '_1 is under progress. Please check the My Models screen for the status update. Once the model training completion notification is received for first model you can resume with second model creation.';
            this._notificationService.success(msg);
          } else {
            this._notificationService.success('Mapping Done Successfully');
          }
        } else {
          this.checkMappedColumns();
        }
      } else if (this.ifTimeSeriesModel) {
        if (this.mappingOptionSelected.hasOwnProperty(this.targetColumnForMap)
          && this.mappingOptionSelected.hasOwnProperty(this.timeSeriesDateColumn)) {
          if (this.isTimeSeresDateColumnMapped) {
            this.dialogRef.close(objData);
            if (localStorage.getItem('oldCorrelationID') === 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6') {
              let modelName = this.localStorageService.modelName;
              let msg = 'Model training of first model, ' + modelName + '_1 is under progress. Please check the My Models screen for the status update. Once the model training completion notification is received for first model you can resume with second model creation.';
              this._notificationService.success(msg);
            } else {
              this._notificationService.success('Mapping Done Successfully');
            }
          } else {
            this._notificationService.error(`DataType for selected column "' + this.timeSeriesDateColumn + '" should be date. 	
            if Date column is not available please upload different file.`);
          }
        } else {
          this.checkMappedColumns();
        }
      }
    } else {
      this._notificationService.error('Please Apply Mapping');
    }
  }
  // added for defect 994117	
  checkMappedColumns() {
    const iterator = this.existingColumnList;
    for (let j = 0; j < iterator.length; j++) {
      if (this.targetColumnForMap === iterator[j]) {
        // this.isTargetColMapped = false;	
        this.selectedIndex = j;
      }
      if (!this.ifTimeSeriesModel) {
        if (this.uniqueIdentifierVal === iterator[j]) {
          // this.isUniqueIdentifierMap = false;	
          this.selectedUniqueIndex = j;
        }
      } else if (this.ifTimeSeriesModel) {
        if (this.timeSeriesDateColumn === iterator[j]) {
          // this.isTimeSeresDateColumnMapped = false;	
          this.selectedTimeDateColIndex = j;
        }
      }
    }
    if (this.isTargetColMapped === false) {
      this._notificationService.error('Please Map Target Column "' + this.targetColumnForMap + '" Mark As Red');
    }
    if (!this.ifTimeSeriesModel) {
      if (this.isUniqueIdentifierMap === false) {
        this._notificationService.error('Please Map Unique Identifier Column "' + this.uniqueIdentifierVal + '" Mark As Red');
      }
    } else if (this.ifTimeSeriesModel) {
      if (this.isTimeSeresDateColumnMapped === false) {
        this._notificationService.error('Please Map DATE Column For Time Series Frequency "' +
          this.timeSeriesDateColumn + '" Mark As Red');
      }
    }
  }

  addCustomColumn(data) {
    this.resetValue = false;
    this.customcolumn = data.trim();
    if (this.customcolumn) {
      if (this.existingColumnList.includes(this.customcolumn)) {
        this._notificationService.error(this.customcolumn +
          ' Already Added. Please change the Column Name');
        return '';
      }
      this.existingColumnList.push(this.customcolumn);

      this.existingColumnList.forEach((val) => {
        const tempMappingOption = Object.assign({}, this.mappingOptionSelected);
        this.featuresColumnForMapping[val] = new Set(this.featuresColumnForMapping['temp']);
        delete tempMappingOption[val];
        // tslint:disable: no-shadowed-variable
        Object.keys(tempMappingOption).map(item => tempMappingOption[item]).forEach(
          data => this.featuresColumnForMapping[val].delete(data)
        );
      });
      if (this.mappingOptionSelected.hasOwnProperty(this.targetColumnForMap)) {
        this.isTargetColMapped = true;
      }
      if (!this.ifTimeSeriesModel) {
        if (this.mappingOptionSelected.hasOwnProperty(this.uniqueIdentifierVal)) {
          this.isUniqueIdentifierMap = true;
        }
      } else if (this.ifTimeSeriesModel) {
        if (this.mappingOptionSelected.hasOwnProperty(this.timeSeriesDateColumn)) {
          this.isTimeSeresDateColumnMapped = true;
        }
      }
    } else {
      this._notificationService.error('please enter value');
    }
    this.toggleCustomColumn();
  }

  toggleCustomColumn() {
    this.resetValue = false;
    this.displayaddCustomColumn = !this.displayaddCustomColumn;
  }

  resetMappingColumns() {
    this.featuresColumnForMapping = {};
    this.mappingOptionSelected = {};
    this.existingColumnList.forEach((value) =>
      this.featuresColumnForMapping[value] = this.featureListForReset ? new Set(this.featureListForReset) : new Set([])
    );
    this.featuresColumnForMapping['temp'] = this.featureListForReset ? new Set(this.featureListForReset) : new Set([]);
    this.resetValue = true;
  }

  delete(value, index) {
    // BugFix 816956
    this.existingColumnList.splice(index, 1);
    let deletedVal = this.mappingOptionSelected[value];
    delete this.featuresColumnForMapping[value];
    if (!this._coreUtilService.isNil(deletedVal)) {
      delete this.mappingOptionSelected[value];
      this.existingColumnList.forEach((val) => {
        this.featuresColumnForMapping[val].add(deletedVal);
      });
    }

    // this.onMappingOptionSelected(value, index);
  }

  validRecordDetailsPopup(validRecordDetails) {
    if (validRecordDetails.Records[0] >= 4 && validRecordDetails.Records[0] < 20) {
      this.validRecMessage = `The accuracy of the model will be impacted as less than 20 records are available for training. 
      Do you wish to proceed?`;
      this.ifLessRecords = true;
    } else if (validRecordDetails.Records[0] >= 20) {
      this.validRecMessage = validRecordDetails['Msg'];
    }

    this.modalRef = this._modalService.show(this.confirmValidRecModels, this.config);
  }

  redirectToDashboard() {
    if (!this._coreUtilService.isNil(this.correlationId)) {
      this._appUtilsService.loadingStarted();
      this._problemStatementService.deleteModelByCorrelationId(this.correlationId).subscribe(
        data => {
          this._appUtilsService.loadingEnded();
          this.modalRef.hide();
          this.onClose();
          let url = '/choosefocusarea';
          this.router.navigateByUrl(url);
        }, error => {
          this._appUtilsService.loadingEnded();
        });
    }
  }


  onConfirm() {
    this.modalRef.hide();
  }
}
