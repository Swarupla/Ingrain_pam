import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { timer } from 'rxjs';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { AdProblemStatementService } from 'src/app/_services/ad-problem-statement.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-upload-progress',
  templateUrl: './upload-progress.component.html',
  styleUrls: ['./upload-progress.component.scss']
})
export class UploadProgressComponent implements OnInit {
  progress;
  message: string;
  agileFlag: boolean;
  paramData: any;
  dataForMapping;
  isModelTemplateDataSource = false;
  timerSubscripton: any;
  correlationId_status: string;
  dialogdata: any;
  retryData: any;
  useCaseId = undefined;

  constructor(public _dialogConfig: DialogConfig,
    public _dialogRef: DialogRef, private coreUtilsService: CoreUtilsService,
    private adproblemStatementService: AdProblemStatementService,
    private _notificationService: NotificationService,
    private _appUtilsService: AppUtilsService,
    private route: ActivatedRoute) {
    this.route.queryParams
      .subscribe(params => {
        if (!this.coreUtilsService.isNil(params.displayUploadandDataSourceBlock)) {
          this.isModelTemplateDataSource = params.displayUploadandDataSourceBlock;
        }
      });
  }

  ngOnInit(): void {
    if (this._dialogConfig.data.filesData[0].hasOwnProperty('size')) {
      if (this._dialogConfig.data.filesData[0]['size'] !== '' && this._dialogConfig.data.filesData[0]['size'] > 136356582) {
        this._notificationService.error('Kindly upload file of size less than 130MB.');
        this._dialogRef.close();
      }
    }

    if (this._dialogConfig.data.correlationId === '' || this._dialogConfig.data.correlationId === undefined) {
      // sessionStorage.setItem('dialogdata', this._dialogConfig.data.filesData);
      this.dialogdata = JSON.parse(JSON.stringify(this._dialogConfig.data.filesData));
      this.upload(this._dialogConfig.data.filesData, this._dialogConfig.data.modelName);
    } else {
      this.dialogdata = JSON.parse(JSON.stringify(this._dialogConfig.data.filesData));
      this.uploadDataSourceChangeFile(this._dialogConfig.data.filesData, this._dialogConfig.data.modelName,
        this._dialogConfig.data.correlationId);
    }
  }

  upload(filesData, modelName) {
    let requestParams;
    const parentFileName = filesData[0].parentFileName;
    const dataSource = filesData[0].source;
    const sourceCount = filesData[0].sourceTotalLength;
    const category = filesData[0].category;
    const PIConfirmation = filesData[0].PIConfirmation;
    const EntityStartDate = filesData[0].entityStartDate;
    const EntityEndDate = filesData[0].entityEndDate;
    const oldCorrelationID = filesData[0].oldCorrelationId;
    const language = filesData[0].languageFlag;
    const correlationId_status = this.correlationId_status;
    const datasetid = filesData[0].DatasetUId;
    if (this.isModelTemplateDataSource === undefined || this.isModelTemplateDataSource === null) {
      this.isModelTemplateDataSource = false;
    }
    if (String(this.isModelTemplateDataSource) === 'true' && this.useCaseId === undefined) {
      this.useCaseId = localStorage.getItem('oldCorrelationID');
    }
    this._appUtilsService.getParamData().subscribe(data => this.paramData = data);

    if (dataSource !== 'Dataset' && datasetid === undefined) {
      if (EntityStartDate !== undefined && EntityEndDate !== undefined) {
        requestParams = {
          'ModelName': modelName.toString().trim(),
          'userId': this._appUtilsService.getCookies().UserId,
          'clientUID': this.paramData.clientUID,
          'deliveryUID': this.paramData.deliveryConstructUId,
          'ParentFileName': parentFileName,
          'source': dataSource,
          'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
          'category': category,
          'MappingFlag': 'False',
          'uploadType': 'FileDataSource',
          'DBEncryption': false,
          'E2EUID': sessionStorage.getItem('End2EndId'),
          'ClusterFlag': 'True',
          'EntityStartDate': EntityStartDate,
          'EntityEndDate': EntityEndDate,
          'oldCorrelationID': oldCorrelationID,
          'Language': language,
          'IsModelTemplateDataSource': this.isModelTemplateDataSource,
          'correlationId_status': correlationId_status,
          'usecaseId': this.useCaseId
        };
      } else {
        requestParams = {
          'ModelName': modelName.toString().trim(),
          'userId': this._appUtilsService.getCookies().UserId,
          'clientUID': this.paramData.clientUID,
          'deliveryUID': this.paramData.deliveryConstructUId,
          'ParentFileName': parentFileName,
          'source': dataSource,
          'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
          'category': category,
          'MappingFlag': 'False',
          'uploadType': 'FileDataSource',
          'DBEncryption': PIConfirmation,
          'E2EUID': sessionStorage.getItem('End2EndId'),
          'ClusterFlag': 'False',
          'EntityStartDate': undefined,
          'EntityEndDate': undefined,
          'oldCorrelationID': oldCorrelationID,
          'Language': language,
          'IsModelTemplateDataSource': this.isModelTemplateDataSource,
          'correlationId_status': correlationId_status,
          'usecaseId': this.useCaseId
        };
      }
    } else {
      requestParams = {
        'ModelName': modelName.toString().trim(),
        'userId': this._appUtilsService.getCookies().UserId,
        'clientUID': this.paramData.clientUID,
        'deliveryUID': this.paramData.deliveryConstructUId,
        'ParentFileName': parentFileName,
        'source': dataSource,
        'UploadFileType': 'Single',
        'category': category,
        'MappingFlag': 'False',
        'uploadType': 'Dataset',
        'DBEncryption': PIConfirmation,
        'E2EUID': sessionStorage.getItem('End2EndId'),
        'ClusterFlag': 'False',
        'EntityStartDate': undefined,
        'EntityEndDate': undefined,
        'oldCorrelationID': oldCorrelationID,
        'Language': language,
        'IsModelTemplateDataSource': this.isModelTemplateDataSource,
        'correlationId_status': correlationId_status,
        'usecaseId': this.useCaseId
      };
    }
    this.dataForMapping = requestParams;
    this.adproblemStatementService.uploadFilesAD(filesData, requestParams)
      .subscribe(
        data => {
          if (this.correlationId_status === undefined) {
            this.progress = data.percentDone + '%';
            this.message = data.message;
            if (data.percentDone === 100) {
              this.message = 'Upload in progress....';
            }
          }
          if (data.body) {
            if (data.body.Status) {
              if (data.body.Status === 'I' || data.body.Status === 'E') {
                this._notificationService.error(data.body.Category.Message);
                this._dialogRef.close();
              } else if (data.body.Status === 'P') {
                this.correlationId_status = data.body.correlationId;
                this.retry();
              }
            } else {
              data['dataForMapping'] = this.dataForMapping;
              sessionStorage.removeItem('viewEditAccess');
              this._dialogRef.close(data);
              this._notificationService.success('Data Processed Succesfully');
              this.unsubscribe();
            }
          }
        }, error => {
          if (error.error.hasOwnProperty('Category')) {
            this._notificationService.error(error.error.Category.Message);
          } else {
            this._notificationService.error('Something went wrong.');
          }
          this._dialogRef.close();
        });
  }

  retry() {
    this.progress = 100 + '%';
    this.message = 'Upload in progress....';
    this.timerSubscripton = timer(2000).subscribe(() => this.upload(this.dialogdata, this._dialogConfig.data.modelName));
    return this.timerSubscripton;
  }

  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }


  // Upload Data source change starts.
  uploadDataSourceChangeFile(filesData, modelName, correlationId) {
    let params;
    const parentFileName = filesData[0].parentFileName;
    const dataSource = filesData[0].source;
    const sourceCount = filesData[0].sourceTotalLength;
    const category = filesData[0].category;
    const PIConfirmation = filesData[0].PIConfirmation;
    const EntityStartDate = filesData[0].entityStartDate;
    const EntityEndDate = filesData[0].entityEndDate;
    const oldCorrelationID = filesData[0].oldCorrelationId;
    const language = filesData[0].languageFlag;
    const correlationId_status = this.correlationId_status;
    const datasetid = filesData[0].DatasetUId;
    if (localStorage.getItem('oldCorrelationID') === 'f2e5af7b-e504-4d21-9bb7-f50a1c8f90d6') {
      this.useCaseId = localStorage.getItem('oldCorrelationID');
    }
    this._appUtilsService.getParamData().subscribe(data => this.paramData = data);
    if (dataSource !== 'Dataset' && datasetid === undefined) {
      if (EntityStartDate !== undefined && EntityEndDate !== undefined) {
        params = {
          'ModelName': modelName.toString().trim(),
          'correlationId': correlationId,
          'userId': this._appUtilsService.getCookies().UserId,
          'clientUID': this.paramData.clientUID,
          'deliveryUID': this.paramData.deliveryConstructUId,
          'ParentFileName': parentFileName,
          'source': dataSource,
          'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
          'category': category,
          'MappingFlag': 'False',
          'uploadType': 'UploadDataSource',
          'DBEncryption': PIConfirmation,
          'E2EUID': sessionStorage.getItem('End2EndId'),
          'ClusterFlag': 'True',
          'EntityStartDate': EntityStartDate,
          'EntityEndDate': EntityEndDate,
          'oldCorrelationID': oldCorrelationID,
          'Language': language,
          'correlationId_status': correlationId_status,
          'usecaseId': this.useCaseId
        };
      } else {
        params = {
          'ModelName': modelName.toString().trim(),
          'correlationId': correlationId,
          'userId': this._appUtilsService.getCookies().UserId,
          'clientUID': this.paramData.clientUID,
          'deliveryUID': this.paramData.deliveryConstructUId,
          'ParentFileName': parentFileName,
          'source': dataSource,
          'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
          'category': category,
          'MappingFlag': 'False',
          'uploadType': 'UploadDataSource',
          'DBEncryption': PIConfirmation,
          'E2EUID': sessionStorage.getItem('End2EndId'),
          'ClusterFlag': 'False',
          'EntityStartDate': undefined,
          'EntityEndDate': undefined,
          'oldCorrelationID': oldCorrelationID,
          'Language': language,
          'correlationId_status': correlationId_status,
          'usecaseId': this.useCaseId
        };
      }
    } else {
      params = {
        'ModelName': modelName.toString().trim(),
        'correlationId': correlationId,
        'userId': this._appUtilsService.getCookies().UserId,
        'clientUID': this.paramData.clientUID,
        'deliveryUID': this.paramData.deliveryConstructUId,
        'ParentFileName': parentFileName,
        'source': dataSource,
        'UploadFileType': 'Single',
        'category': category,
        'MappingFlag': 'False',
        'uploadType': 'Dataset',
        'DBEncryption': PIConfirmation,
        'E2EUID': sessionStorage.getItem('End2EndId'),
        'ClusterFlag': 'False',
        'EntityStartDate': undefined,
        'EntityEndDate': undefined,
        'oldCorrelationID': oldCorrelationID,
        'Language': language,
        'IsModelTemplateDataSource': this.isModelTemplateDataSource,
        'correlationId_status': correlationId_status,
        'usecaseId': this.useCaseId
      };
    }
    this.dataForMapping = params;
    this.adproblemStatementService.dataSourceFileUpload(filesData, params).subscribe(
      data => {
        if (this.correlationId_status === undefined) {
          this.progress = data.percentDone + '%';
          this.message = data.message;
          if (data.percentDone === 100) {
            this.message = 'Upload in progress....';
          }
        }
        if (data.body) {
          if (data.body.Status) {
            if (data.body.Status === 'I' || data.body.Status === 'E') {
              this._notificationService.error(data.body.Category.Message);
              this._dialogRef.close();
            } else if (data.body.Status === 'P') {
              this.correlationId_status = data.body.correlationId;
              this.retryDataSource();
            } else {
              data['dataForMapping'] = this.dataForMapping;
              this._dialogRef.close(data);
            }
          } else {
            data['dataForMapping'] = this.dataForMapping;
            sessionStorage.removeItem('viewEditAccess');
            this._dialogRef.close(data);
            this._notificationService.success('Data Processed Succesfully');
            this.unsubscribeDataSource();
          }
        }
      }, error => {
        if (error.error.hasOwnProperty('Category')) {
          this._notificationService.error(error.error.Category.Message);
        } else if (error.status === 500) {
          this._notificationService.error(error.error);
        } else {
          this._notificationService.error('Something went wrong.');
        }
        this._dialogRef.close();
      });
  }

  retryDataSource() {
    this.progress = 100 + '%';
    this.message = 'Upload in progress....';
    this.timerSubscripton = timer(2000).subscribe(() => this.uploadDataSourceChangeFile(this.dialogdata, this._dialogConfig.data.modelName,
      this._dialogConfig.data.correlationId));
    return this.timerSubscripton;
  }

  unsubscribeDataSource() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }
}
