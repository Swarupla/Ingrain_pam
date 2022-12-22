import { Component, OnInit } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { FileUploadProgressBarComponent } from 'src/app/components/file-upload-progress-bar/file-upload-progress-bar.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
// import { tap } from 'rxjs/operators';
import { Router, ActivatedRoute } from '@angular/router';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { timer } from 'rxjs';

@Component({
  selector: 'app-files-mapping-modal',
  templateUrl: './files-mapping-modal.component.html',
  styleUrls: ['./files-mapping-modal.component.scss']
})
export class FilesMappingModalComponent implements OnInit {

  constructor(private dr: DialogRef, private dc: DialogConfig, private dialogService: DialogService,
    private ls: LocalStorageService, private router: Router, private _problemStatementService: ProblemStatementService,
    private _appUtilsService: AppUtilsService, private _notificationService: NotificationService, private coreUtilsService: CoreUtilsService) { }
  statusFlag;
  timerSubscripton: any;
  preMappingdata;
  parentFile;
  parentFileColumns;
  mappingColumns;
  correlationId;
  sourceColumn0;
  mappingColumns0;
  mappingJoin0 = ['left', 'inner'];
  sourceColumns1;
  mappingColumns1;
  mappingJoin1 = ['left', 'inner'];
  sourceColumns2;
  mappingColumns2;
  mappingJoin2 = ['left', 'inner'];
  sourceColumns3;
  mappingColumns3;
  mappingJoin3 = ['left', 'inner'];
  sourceColumns4;
  mappingColumns4;
  mappingJoin4 = ['left', 'inner'];
  allFiles;
  isLoading = false;
  mapping0 = {
    'source_file': '',
    'source_column': '',
    'mapping_file': '',
    'mapping_column': '',
    'mapping_join': 'left'
  };
  mapping1 = {
    'source_file': '',
    'source_column': '',
    'mapping_file': '',
    'mapping_column': '',
    'mapping_join': 'left'
  };
  mapping2 = {
    'source_file': '',
    'source_column': '',
    'mapping_file': '',
    'mapping_column': '',
    'mapping_join': 'left'
  };
  mapping3 = {
    'source_file': '',
    'source_column': '',
    'mapping_file': '',
    'mapping_column': '',
    'mapping_join': 'left'
  };
  mapping4 = {
    'source_file': '',
    'source_column': '',
    'mapping_file': '',
    'mapping_column': '',
    'mapping_join': 'left'
  };
  processedData = {};
  fileUploadData;
  fileGeneralData;
  disableApply = false;
  isOnlyFiles = false;

  ngOnInit() {
    this.loadPreMappingData(this.dc.data);
    this.fileUploadData = this.dc.data.fileUploadData;
    this.fileGeneralData = this.dc.data.fileGeneralData;
  }

  loadPreMappingData(filesData) {
    this.preMappingdata = filesData.filesData;
    this.isOnlyFiles = this.preMappingdata.Fileflag;
    this.parentFile = this.preMappingdata.File.filter(x => { if (x.ParentFileFlag === true) { return x; } });
    if (this.parentFile.length === 0) {
      this._notificationService.error('Parent file not selected.');
      this.dr.close();
    } else {
      this.parentFileColumns = Object.keys(this.parentFile[0].FileColumn);
      this.allFiles = this.preMappingdata.File;
      const sourceFile = this.preMappingdata.ParentFileName.split('.');
      this.mapping0.source_file = sourceFile[0];
    }
  }

  onClose() {
    this.dr.close();
  }

  onSelectFileName(event) {
    const selectedFile = event.target.value;
    const fileId = event.target.id;
    const index = fileId.substring(fileId.length, fileId.length - 1);
    const id = fileId.substring((fileId.length - 1), 0);
    const mappingFile = this.preMappingdata.File.filter(x => { if (x.FileName === selectedFile) { return x; } });
    if (mappingFile.length === 0) {
      this._notificationService.error('Kindly choose a valid file.');
    } else {
      const exist = this.checkExistingCombo(index);
      if (exist === true) {
        if (id === 'source_file') {
          this['mapping' + index].source_file = '';
          this['sourceColumns' + index] = [];
        } else if (id === 'mapping_file') {
          this['mapping' + index].mapping_file = '';
          this['mappingColumns' + index] = [];
        }
        event.target.value = '';
        this._notificationService.error('You have already mapped columns for this file combination. Kindly choose other combination.');
      } else {
        if (index === '0') {
          this.mappingColumns0 = Object.keys(mappingFile[0].FileColumn);
          if (id === 'mapping_file') {
            if (this.mapping0.source_column !== '') {
              const selectedcolumn = this.mapping0.source_column;
              const sourceColumns = this.preMappingdata.File.filter(x => {
                if (x.FileName === this.mapping0.source_file) {
                  return x.FileColumn;
                }
              });
              const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
              const mappingColumns = Object.keys(mappingFile[0].FileColumn);
              const newMappingColumns = [];
              for (let i = 0; i < mappingColumns.length; i++) {
                const column = mappingColumns[i];
                if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                  newMappingColumns.push(column);
                }
              }
              this.mappingColumns0 = Object.assign([], newMappingColumns);
            }
          }
        }
        if (index === '1') {
          if (id === 'source_file') {
            this.sourceColumns1 = Object.keys(mappingFile[0].FileColumn);
          } else if (id === 'mapping_file') {
            this.mappingColumns1 = Object.keys(mappingFile[0].FileColumn);
            if (this.mapping1.source_column !== '') {
              const selectedcolumn = this.mapping1.source_column;
              const sourceColumns = this.preMappingdata.File.filter(x => {
                if (x.FileName === this.mapping1.source_file) {
                  return x.FileColumn;
                }
              });
              const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
              const mappingColumns = Object.keys(mappingFile[0].FileColumn);
              const newMappingColumns = [];
              for (let i = 0; i < mappingColumns.length; i++) {
                const column = mappingColumns[i];
                if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                  newMappingColumns.push(column);
                }
              }
              this.mappingColumns1 = Object.assign([], newMappingColumns);
            }
          }
        }
        if (index === '2') {
          if (id === 'source_file') {
            this.sourceColumns2 = Object.keys(mappingFile[0].FileColumn);
          } else if (id === 'mapping_file') {
            this.mappingColumns2 = Object.keys(mappingFile[0].FileColumn);
            if (this.mapping2.source_column !== '') {
              const selectedcolumn = this.mapping2.source_column;
              const sourceColumns = this.preMappingdata.File.filter(x => {
                if (x.FileName === this.mapping2.source_file) {
                  return x.FileColumn;
                }
              });
              const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
              const mappingColumns = Object.keys(mappingFile[0].FileColumn);
              const newMappingColumns = [];
              for (let i = 0; i < mappingColumns.length; i++) {
                const column = mappingColumns[i];
                if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                  newMappingColumns.push(column);
                }
              }
              this.mappingColumns2 = Object.assign([], newMappingColumns);
            }
          }
        }
        if (index === '3') {
          if (id === 'source_file') {
            this.sourceColumns3 = Object.keys(mappingFile[0].FileColumn);
          } else if (id === 'mapping_file') {
            this.mappingColumns3 = Object.keys(mappingFile[0].FileColumn);
            if (this.mapping3.source_column !== '') {
              const selectedcolumn = this.mapping3.source_column;
              const sourceColumns = this.preMappingdata.File.filter(x => {
                if (x.FileName === this.mapping3.source_file) {
                  return x.FileColumn;
                }
              });
              const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
              const mappingColumns = Object.keys(mappingFile[0].FileColumn);
              const newMappingColumns = [];
              for (let i = 0; i < mappingColumns.length; i++) {
                const column = mappingColumns[i];
                if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                  newMappingColumns.push(column);
                }
              }
              this.mappingColumns3 = Object.assign([], newMappingColumns);
            }
          }
        }
        if (index === '4') {
          if (id === 'source_file') {
            this.sourceColumns4 = Object.keys(mappingFile[0].FileColumn);
          } else if (id === 'mapping_file') {
            this.mappingColumns4 = Object.keys(mappingFile[0].FileColumn);
            if (this.mapping0.source_column !== '') {
              const selectedcolumn = this.mapping4.source_column;
              const sourceColumns = this.preMappingdata.File.filter(x => {
                if (x.FileName === this.mapping4.source_file) {
                  return x.FileColumn;
                }
              });
              const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
              const mappingColumns = Object.keys(mappingFile[0].FileColumn);
              const newMappingColumns = [];
              for (let i = 0; i < mappingColumns.length; i++) {
                const column = mappingColumns[i];
                if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                  newMappingColumns.push(column);
                }
              }
              this.mappingColumns4 = Object.assign([], newMappingColumns);
            }
          }
        }
      }
    }
  }

  onColumnSelect(event) {
    const selectedcolumn = event.target.value;
    const fileId = event.target.id;
    const index = fileId.substring(fileId.length, fileId.length - 1);
    const id = fileId.substring((fileId.length - 1), 0);
    if (selectedcolumn !== '') {
      if (index === '0') {
        this.processedData['mapping0'] = this.mapping0;
        if (this.mapping0.mapping_file !== '') {
          if (id === 'source_column') {
            const sourceColumns = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping0.source_file) {
                return x.FileColumn;
              }
            });
            const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
            const mappingFile = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping0.mapping_file) {
                return x.FileColumn;
              }
            });
            const mappingColumns = Object.keys(mappingFile[0].FileColumn);
            const newMappingColumns = [];
            for (let i = 0; i < mappingColumns.length; i++) {
              const column = mappingColumns[i];
              if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                newMappingColumns.push(column);
              }
            }
            this.mappingColumns0 = Object.assign([], newMappingColumns);
          }
        }
      }
      if (index === '1') {
        this.processedData['mapping1'] = this.mapping1;
        if (this.mapping1.mapping_file !== '') {
          if (id === 'source_column') {
            const sourceColumns = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping1.source_file) {
                return x.FileColumn;
              }
            });
            const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
            const mappingFile = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping1.mapping_file) {
                return x.FileColumn;
              }
            });
            const mappingColumns = Object.keys(mappingFile[0].FileColumn);
            const newMappingColumns = [];
            for (let i = 0; i < mappingColumns.length; i++) {
              const column = mappingColumns[i];
              if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                newMappingColumns.push(column);
              }
            }
            this.mappingColumns1 = Object.assign([], newMappingColumns);
          }
        }
      }
      if (index === '2') {
        this.processedData['mapping2'] = this.mapping2;
        if (this.mapping2.mapping_file !== '') {
          if (id === 'source_column') {
            const sourceColumns = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping2.source_file) {
                return x.FileColumn;
              }
            });
            const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
            const mappingFile = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping2.mapping_file) {
                return x.FileColumn;
              }
            });
            const mappingColumns = Object.keys(mappingFile[0].FileColumn);
            const newMappingColumns = [];
            for (let i = 0; i < mappingColumns.length; i++) {
              const column = mappingColumns[i];
              if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                newMappingColumns.push(column);
              }
            }
            this.mappingColumns2 = Object.assign([], newMappingColumns);
          }
        }
      }
      if (index === '3') {
        this.processedData['mapping3'] = this.mapping3;
        if (this.mapping3.mapping_file !== '') {
          if (id === 'source_column') {
            const sourceColumns = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping3.source_file) {
                return x.FileColumn;
              }
            });
            const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
            const mappingFile = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping3.mapping_file) {
                return x.FileColumn;
              }
            });
            const mappingColumns = Object.keys(mappingFile[0].FileColumn);
            const newMappingColumns = [];
            for (let i = 0; i < mappingColumns.length; i++) {
              const column = mappingColumns[i];
              if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                newMappingColumns.push(column);
              }
            }
            this.mappingColumns3 = Object.assign([], newMappingColumns);
          }
        }
      }
      if (index === '4') {
        this.processedData['mapping4'] = this.mapping4;
        if (this.mapping4.mapping_file !== '') {
          if (id === 'source_column') {
            const sourceColumns = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping4.source_file) {
                return x.FileColumn;
              }
            });
            const sourceColumnDataType = sourceColumns[0].FileColumn[selectedcolumn];
            const mappingFile = this.preMappingdata.File.filter(x => {
              if (x.FileName === this.mapping4.mapping_file) {
                return x.FileColumn;
              }
            });
            const mappingColumns = Object.keys(mappingFile[0].FileColumn);
            const newMappingColumns = [];
            for (let i = 0; i < mappingColumns.length; i++) {
              const column = mappingColumns[i];
              if (mappingFile[0].FileColumn[column] === sourceColumnDataType) {
                newMappingColumns.push(column);
              }
            }
            this.mappingColumns4 = Object.assign([], newMappingColumns);
          }
        }
      }
    } else {
      if (index === '0') {
        delete this.processedData['mapping0'];
      }
      if (index === '1') {
        delete this.processedData['mapping1'];
      }
      if (index === '2') {
        delete this.processedData['mapping2'];
      }
      if (index === '3') {
        delete this.processedData['mapping3'];
      }
      if (index === '4') {
        delete this.processedData['mapping4'];
      }
    }
  }

  uploadMappingData() {
    if (Object.keys(this.processedData).length > 0) {
      this.disableApply = true;
      this._appUtilsService.loadingStarted();
      const requestParams = {
        'userId': this._appUtilsService.getCookies().UserId,
        'UploadFileType': 'multiple',
        'MappingFlag': 'True',
        'correlationId': this.preMappingdata.CorrelationId,
        'ModelName': this.fileGeneralData.ModelName,
        'clientUID': this.fileGeneralData.clientUID,
        'deliveryUID': this.fileGeneralData.deliveryUID,
        'category': this.fileGeneralData.category,
        'uploadType': this.fileGeneralData.uploadType,
        'statusFlag': this.statusFlag
      };
        // Inference Engine :+: Multiple Upload Code
        const inferenceEngine = this.dc.data.inferenceEngine 

      this._problemStatementService.uploadMappingColumns(this.processedData, requestParams, this.fileUploadData, inferenceEngine)
        .subscribe(
          data => {
            if (data.Status === 'P') {
              this.statusFlag = 'True';
              this.retry();
            } else if (data.Status === 'E' || data.Status === 'I') {
              this._appUtilsService.loadingImmediateEnded();
              if ( data.hasOwnProperty('Message')) {
              this._notificationService.error(data.Message);
              } else {
                this._notificationService.error(data);  
              }
            } else {
            this._appUtilsService.loadingImmediateEnded();
            this.ls.setLocalStorageData('correlationId', data[0]);
            this.dr.close(data);
            this._notificationService.success('Data Processed Succesfully');
            }
          }, error => {
            this._appUtilsService.loadingImmediateEnded();
            this.disableApply = false;
            if (error.error.hasOwnProperty('Category')) {
              this._notificationService.error(error.error.Category.Message);
              localStorage.removeItem('modelName');
              this.ls.setLocalStorageData('modelName', '');
            } else {

              this._notificationService.error(error.error);
              localStorage.removeItem('modelName');
              this.ls.setLocalStorageData('modelName', '');
            }
            this.dr.close();
          });

    } else {
      this._notificationService.error('Kindly fill at least one row.');
    }
  }

  retry() {   
    this.timerSubscripton = timer(2000).subscribe(() => this.uploadMappingData());  
    return this.timerSubscripton;
  }

  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }

  onJoinSelect(event) {
    const selectedcolumn = event.target.value;
    const fileId = event.target.id;
    const index = fileId.substring(fileId.length, fileId.length - 1);
    const id = fileId.substring((fileId.length - 1), 0);
    let mappingObj;
    if (index === '0') {
      mappingObj = this.mapping0;
    }
    if (index === '1') {
      mappingObj = this.mapping1;
    }
    if (index === '2') {
      mappingObj = this.mapping2;
    }
    if (index === '3') {
      mappingObj = this.mapping3;
    }
    if (index === '4') {
      mappingObj = this.mapping4;
    }
    if (selectedcolumn !== '') {
      this.processedData['mapping' + index] = mappingObj;
      this.processedData['mapping' + index]['mapping_join'] = selectedcolumn;
    } else {
      this.processedData['mapping4'] = mappingObj;
      this.processedData['mapping' + index]['mapping_join'] = 'left';
    }
  }

  checkExistingCombo(index) {
    let exist = false;
    let count = 0;
    for (let i = 0; i < 5; i++) {
      if (i !== parseInt(index, 0)) {
        if (this['mapping' + index].source_file !== '' && this['mapping' + i].source_file !== '' &&
          this['mapping' + index].mapping_file !== '' && this['mapping' + i].mapping_file !== '') {
          if ((this['mapping' + index].source_file === this['mapping' + i].source_file &&
            this['mapping' + index].mapping_file === this['mapping' + i].mapping_file) ||
            (this['mapping' + index].source_file === this['mapping' + i].mapping_file &&
              this['mapping' + index].mapping_file === this['mapping' + i].source_file)) {
            count++;
          }
        }
        if (count > 0) {
          exist = true;
        } else {
          exist = false;
        }
      }
    }
    return exist;
  }
}
