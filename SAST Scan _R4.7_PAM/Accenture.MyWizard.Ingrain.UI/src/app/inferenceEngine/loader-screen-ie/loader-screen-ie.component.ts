import { AfterViewInit, Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { timer } from 'rxjs';
import { tap } from 'rxjs/operators';
import { FilesMappingModalComponent } from 'src/app/components/files-mapping-modal/files-mapping-modal.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

import { UploadIEDataService } from '../inference-engine/upload-IEData.service';

@Component({
  selector: 'app-loader-screen-ie',
  templateUrl: './loader-screen-ie.component.html',
  styleUrls: ['./loader-screen-ie.component.css']
})
export class LoaderScreenIeComponent implements AfterViewInit {
  @Output() loaderFinished = new EventEmitter<any>();
  @Input() requiredUploadIEPayload;
  @Input() modelName;
  @Input() FileEntityName;
  progress;
  message: string;
  agileFlag: boolean;
  paramData: any;
  dataForMapping;
  isModelTemplateDataSource = false;
  timerSubscripton: any;
  correlationId_status: string;
  // dialogdata: any;
  useCaseId = undefined;
  paramsForOpenModalMultipleDialog = {};
  isMappingRequired = false;
  image1 = false;
  image2 = false;
  uploadedAsFile = ''; 
  // public _dialogConfig: DialogConfig, public _dialogRef: DialogRef,
  constructor(private coreUtilsService: CoreUtilsService,
    private _notificationService: NotificationService,
    private _appUtilsService: AppUtilsService,
    private route: ActivatedRoute, private router: Router, private ls: LocalStorageService,
    private uploadIEDataService: UploadIEDataService, private dialogService: DialogService) {
    this.route.queryParams
      .subscribe(params => {
        if (!this.coreUtilsService.isNil(params.displayUploadandDataSourceBlock)) {
          this.isModelTemplateDataSource = params.displayUploadandDataSourceBlock;
        }
      });
  }

  ngAfterViewInit(): void {
    const totalSourceCount = this.requiredUploadIEPayload[0].sourceTotalLength;
    if (totalSourceCount > 1) {
      const data = { filesData: this.requiredUploadIEPayload, modelName: this.modelName };
      this.isMappingRequired = true;
    }
    this.uploadIEData(this.requiredUploadIEPayload, this.modelName);
  }

   /* ------------------- Upload IE Data ------------------------*/
  uploadIEData(filesData, modelName, selectedServiceId?) {


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

    if (this.isModelTemplateDataSource === undefined || this.isModelTemplateDataSource === null) {
      this.isModelTemplateDataSource = false;
    }
    if (String(this.isModelTemplateDataSource) === 'true' && this.useCaseId === undefined) {
      this.useCaseId = localStorage.getItem('oldCorrelationID');
    }
    this._appUtilsService.getParamData().subscribe(data => this.paramData = data);
   
    this.uploadedAsFile = dataSource;

    requestParams = {
      'ModelName': modelName.toString().trim(),
      'userId': this._appUtilsService.getCookies().UserId,
      'clientUID': this.paramData.clientUID,
      'deliveryUID': this.paramData.deliveryConstructUId,
      'E2EUID': sessionStorage.getItem('End2EndId'),
      'IsModelTemplateDataSource': this.isModelTemplateDataSource,
      'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
      'usecaseId': this.useCaseId,
      'ParentFileName': parentFileName,
      'source': dataSource,
      'category': category,
      'DBEncryption': false,
      'oldCorrelationID': oldCorrelationID,
      'Language': language,
      'correlationId_status': correlationId_status,
      'EntityStartDate': undefined,
      'EntityEndDate': undefined,
      'ClusterFlag': 'False',
      'MappingFlag': 'False',
      'uploadType': 'FileDataSource'
    };

    if (EntityStartDate !== undefined && EntityEndDate !== undefined) {
      requestParams = {
        'ModelName': modelName.toString().trim(),
        'userId': this._appUtilsService.getCookies().UserId,
        'usecaseId': this.useCaseId,
        'E2EUID': sessionStorage.getItem('End2EndId'),
        'clientUID': this.paramData.clientUID,
        'deliveryUID': this.paramData.deliveryConstructUId,
        'IsModelTemplateDataSource': this.isModelTemplateDataSource,
        'ParentFileName': parentFileName,
        'source': dataSource,
        'UploadFileType': sourceCount === 1 ? 'single' : 'multiple',
        'category': category,
        'MappingFlag': 'False',
        'uploadType': 'FileDataSource',
        'DBEncryption': false,
        'ClusterFlag': 'True',
        'EntityStartDate': EntityStartDate,
        'EntityEndDate': EntityEndDate,
        'oldCorrelationID': oldCorrelationID,
        'Language': language,
        'correlationId_status': correlationId_status,

      }
    };

    this.dataForMapping = requestParams;
    this.uploadIEDataService.ingestIEData(filesData, requestParams, dataSource, this.FileEntityName)
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
                this.loaderFinishedEmitterEventError();
                // this._dialogRef.close();
              } else if (data.body.Status === 'P') {
                this.correlationId_status = data.body.correlationId;
                this.retryIE();
              } else {
                data['dataForMapping'] = this.dataForMapping;
                if (this.isMappingRequired) {
                  this.openModalMultipleDialog(data.body, this.modelName, this.requiredUploadIEPayload, this.dataForMapping);
                } else {
                  this.afterUploadingDataIsCompleted(this.correlationId_status, data);
                }

              }
            } else {
              data['dataForMapping'] = this.dataForMapping;
              this.paramsForOpenModalMultipleDialog = data;
              sessionStorage.removeItem('viewEditAccess');
              // this._dialogRef.close(data);
              // this._notificationService.success('Data Processed Succesfully');
              if (this.isMappingRequired) {
                this.openModalMultipleDialog(data.body, this.modelName, this.requiredUploadIEPayload, this.dataForMapping);
              } else {
                this.afterUploadingDataIsCompleted(this.correlationId_status, data);
              }
              this.unsubscribe();
            }
          }
        }, error => {
          if (error.error.hasOwnProperty('Category')) {
            this._notificationService.error(error.error.Category.Message);
          } else {
            this._notificationService.error(error.error);
          }
          this.loaderFinishedEmitterEventError();
          // this._dialogRef.close();
        });

  }

  /* ------------------- Retry Upload IE Data ------------------------*/
  retryIE() {
    this.progress = 100 + '%';
    this.message = 'Upload in progress....';
    // this.retryData = sessionStorage.getItem('dialogdata');
    this.timerSubscripton = timer(5000).subscribe(() => this.uploadIEData(this.requiredUploadIEPayload, this.modelName));
    //  this.dialogdata = JSON.parse(JSON.stringify(this.dialogdata));
    return this.timerSubscripton;
  }

  /*------------------------- Multiple File Mapping Dialog------------ */
  openModalMultipleDialog(filesData, modelName, fileUploadData, fileGeneralData) {
    this.modelName = modelName.toString().trim();
    if (filesData.Flag === 'flag3' || filesData.Flag === 'flag4') {
      const openFileMappingTemplateAfterClosed = this.dialogService.open(FilesMappingModalComponent,
        { data: { filesData: filesData, fileUploadData: fileUploadData, fileGeneralData: fileGeneralData, 'inferenceEngine': true } }).afterClosed.pipe(
          tap(data => data ? this.afterUploadingDataIsCompleted(data[0]) : this.loaderFinishedEmitterEventError()
          )
        );
      openFileMappingTemplateAfterClosed.subscribe();
    } else {
      if (filesData.CorrelationId !== undefined) {
        // output data 
        // this.navigateToUseCaseDefinition(filesData.CorrelationId);
        this.afterUploadingDataIsCompleted(filesData.CorrelationId);

      } else {
        if (filesData[0] !== undefined) {
          // output data 
          // this.navigateToUseCaseDefinition(filesData[0]);}
          this.afterUploadingDataIsCompleted(filesData[0]);
        }
      }
    }
  }

  /* -------------------  AutoGenerated  API ------------------------*/
  afterUploadingDataIsCompleted(correlationId: string, data?) {

    // api/AutoGenerateInferences
    this.uploadIEDataService.autoGenerateInferences(correlationId, false).subscribe(
      (data) => {
        if (data && data['Status'] === 'P') {
          this.retryAutoGenarateInferences(correlationId);
        }
        if (data && data['Status'] === 'C') {
          if (this.uploadedAsFile === 'File' && sessionStorage.getItem('Environment') !== 'FDS') {
            this._notificationService.success('File Uploaded Successfully.');
          } else if (this.uploadedAsFile !== 'Custom' && sessionStorage.getItem('Environment') == 'FDS') {
            this._notificationService.success('File Uploaded Successfully.');
          } else {
          this._notificationService.success('Data processed Successfully.')
          }
          this.loaderFinishedEmitterEventSuccess();
          this.unsubscribe();
        }

        if (data && data['Status'] === 'E') {
          this.loaderFinishedEmitterEventError();
          this._notificationService.error(data['Message']);
          this.unsubscribe();
        }
      },
      (error) => {
        this.loaderFinishedEmitterEventError();
        this._notificationService.error(error.error)
        this.unsubscribe();
      }
    )
  }

   /* ------------------- Retry AutoGenerated ------------------------*/
  retryAutoGenarateInferences(correlationId: string) {
    this.message = 'Upload in progress....';
    this.timerSubscripton = timer(5000).subscribe(() => this.afterUploadingDataIsCompleted(correlationId));
    return this.timerSubscripton;
  }

  /*-----------------------UnSubscribe all Retry -----------------------------*/
  unsubscribe() {
    if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
      this.timerSubscripton.unsubscribe();
    }
  }

   /* ------------------- Call back to Calling Component ------------------------*/
  loaderFinishedEmitterEventError() {
    this.loaderFinished.emit({ Status: 'E' });
  }

   /* ------------------- Call back to Calling Component  ------------------------*/
  loaderFinishedEmitterEventSuccess() {
    this.loaderFinished.emit({ Status: 'S' });
  }
}
