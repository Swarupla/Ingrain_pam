import { Component, OnInit, OnDestroy, ElementRef, Inject, ViewChild } from '@angular/core';
import { ProblemStatementService } from '../../../../_services/problem-statement.service';
import { DialogService } from 'src/app/dialog/dialog.service';
import { UploadFileComponent } from 'src/app/components/upload-file/upload-file.component';
import { TemplateNameModalComponent } from 'src/app/components/template-name-modal/template-name-modal.component';
import { Router, ActivatedRoute } from '@angular/router';
import { retry, tap } from 'rxjs/operators';
import { FileUploadProgressBarComponent } from 'src/app/components/file-upload-progress-bar/file-upload-progress-bar.component';
import { observable, Observable, Subscription, timer } from 'rxjs';
import { AppUtilsService } from '../../../../_services/app-utils.service';
import { CoreUtilsService } from '../../../../_services/core-utils.service';
import { RegressionPopupComponent } from '../regression-popup/regression-popup.component';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { TruncatePublicModelnamePipe } from '../../../../_pipes/truncate-public-modelname.pipe';
import { DatePipe } from '@angular/common';
import { browserRefreshforApp } from '../../../root/app.component';
import { UtilityUploadComponent } from 'src/app/components/utility-upload/utility-upload.component';
import { browser } from 'protractor';
import { browserRefresh } from 'src/app/components/header/header.component';
import { HostListener } from '@angular/core';
import { DatasetDeletionPopupComponent } from '../dataset-deletion-popup/dataset-deletion-popup.component';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ViewDatasetrecordsComponent } from '../view-datasetrecords/view-datasetrecords.component';
import { UtilityApiuploadComponent } from 'src/app/components/utility-apiupload/utility-apiupload.component';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import * as $ from 'jquery';
import { THIS_EXPR } from '@angular/compiler/src/output/output_ast';
import { ProblemStatementComponent } from '../problem-statement.component';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { ApiService } from 'src/app/_services/api.service';
import { ExcelService } from 'src/app/_services/excel.service';
declare var userNotification: any;


@Component({
    selector: 'app-my-utilities',
    templateUrl: './my-utilities.component.html',
    styleUrls: ['./my-utilities.component.scss'],
    providers: [TruncatePublicModelnamePipe]
})

export class MyUtilitiesComponent implements OnInit {

    public browserRefresh: boolean;
    page = 1;
    datasetid;
    userId: any;
    datasettableData;
    datasetColumnsList;
    datasetdata;
    modelName: any;
    correlationId: any;
    subscription: Subscription;
    paramData: any;
    clientUId: string;
    deliveryConstructUID: string;
    myModelClass: string;
    linkTemplateClass: string;
    linkMyModelClass: string;
    filteredMetricData;
    timerSubscripton: any;
    filesdataval: any;
    modelnamelist: any[];
    category: 'File';
    datasets;
    tabledatasets;
    excel = [];
    blob;
    issidebartoggle = false;
    totalRecords: number;
    totalRec: number;
    pinUnpinIconsBar = false;
    status;
    searchText;

    tilecategory = 'File';
    tokenType;

    categorytypes = [{ 'name': 'File', 'value': 'File', 'imgPath': './assets/images/si.svg' },
    { 'name': 'API', 'value': 'API', 'imgPath': './assets/images/agile.svg' },
    { 'name': 'Database', 'value': 'Database', 'imgPath': './assets/images/devops.svg' }
    ];
    content;
    isNavBarLabelsToggle = false;
    toggle: boolean = true;
    decimalPoint;
    infoMessage = `<div>
  <b>Export File Password Hint:</b><br>
  &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and your Name is - John Doe then the password would be 0057Jd<br>
  &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
  </div>`;
    instanceType: string;
    env: string;

    constructor(private route: ActivatedRoute, @Inject(ElementRef) private eleRef: ElementRef,
        private problemStatementService: ProblemStatementService, private coreUtilsService: CoreUtilsService,
        private _appUtilsService: AppUtilsService, private ls: LocalStorageService,
        private dialogService: DialogService, private router: Router, public dialog: MatDialog,
        private truncateString: TruncatePublicModelnamePipe, private datePipe: DatePipe, private _notificationService: NotificationService,
        private envService: EnvironmentService, private api: ApiService, private excelService: ExcelService
    ) {
        this.decimalPoint = sessionStorage.getItem('decimalPoint') ? parseInt(sessionStorage.getItem('decimalPoint')) : 2;
    }

    ngOnInit() {
        // var notifi = new NotificationData();
        // notifi.openDisclaimer();
        this.instanceType = sessionStorage.getItem('Instance');
        this.env = sessionStorage.getItem('Environment');
        this.api.openDisclaimer();
        this.tilecategory = 'File';
        this.myModelClass = 'tab-pane fade active show';
        this.linkMyModelClass = 'nav-link active show';
        this.userId = this._appUtilsService.getCookies().UserId;
        this.subscription = this._appUtilsService.getParamData().subscribe(paramData => {
            this.paramData = paramData;
            this.clientUId = paramData.clientUID;
            this.deliveryConstructUID = paramData.deliveryConstructUId;
            this.browserRefresh = browserRefresh;
        });

        this.getDatasetdetails();
    }

    getDatasetdetails() {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.getDatasetdetails(this.clientUId, this.deliveryConstructUID, this.userId, this.tilecategory)
            .subscribe(data => {
                this._appUtilsService.loadingEnded();
                this.datasets = data;
                this.datasets.forEach(element => {
                    if (this.tilecategory !== 'API') {
                        if (element.SourceDetails === null || element.SourceDetails.FileDetail.length === 0) {
                            element.SourceDetails = 'NA';
                        } else {
                            element.SourceDetails = element.SourceDetails.FileDetail[0]['FileName'];
                        }
                    } else {
                        if (element.SourceDetails.ExternalAPIDetail !== null) {
                            element.AuthType = element.SourceDetails.ExternalAPIDetail.AuthType;
                            element.SourceDetails = element.SourceDetails.ExternalAPIDetail.Url;
                        } else {
                            element.SourceDetails = 'NA';
                        }
                    }
                    if (element.Status === 'P' || element.Status === 'I') {
                        element.Status = 'In Progress (' + element.Progress + ')';
                    } else if (element.Status === 'C') {
                        element.Status = 'Completed';
                    } else {
                        element.Status = 'Error';
                    }
                });
                this.tabledatasets = this.datasets;
            });
    }

    retry() {
        this.timerSubscripton = timer(2000).subscribe(() =>
            this.fetchdatasetdetails());
        return this.timerSubscripton;
    }

    @HostListener('window:beforeunload', ['$event'])
    onWindowClose(event) {
        if (sessionStorage.getItem('progress') === 'Inprocess') {
            event.preventDefault();
            event.returnValue = true;
        }
    }


    downloadData(dataSetUId, FileName) {
        const component = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    component.problemStatementService.downloaddatasource(dataSetUId).subscribe((data: any) => {
                        component.excelService.exportAsExcelFile(data, FileName);
                    }, error => {
                        component._notificationService.error(error);
                    });
                    return false;
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                component.problemStatementService.downloaddatasource(dataSetUId).subscribe((data: any) => {
                    component.excelService.exportAsPasswordProtectedExcelFile(data, FileName).subscribe(response => {
                        component._notificationService.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                        component._notificationService.success('Data Downloaded successfully');
                        let binaryData = [];
                        binaryData.push(response);
                        let downloadLink = document.createElement('a');
                        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                        downloadLink.setAttribute('download', FileName + '.zip');
                        document.body.appendChild(downloadLink);
                        downloadLink.click();
                    });
                }, error => {
                    component._notificationService.error(error);
                });
                return false;
            });
        }
    }


    openModalPopUp() {

        if (this.tilecategory === 'File') {
            if (sessionStorage.getItem('progress') === 'Inprocess') {
                this._notificationService.error('Fileupload already in progress, please try after sometime');
            } else {
                const apiUploadAfterClosed = this.dialogService.open(UtilityUploadComponent,
                    {
                        data: {
                            clientUID: this.paramData.clientUID,
                            deliveryConstructUID: this.deliveryConstructUID,
                            userId: this.userId,
                            category: this.category
                            // fromApp: this.fromApp
                        }
                    }).afterClosed
                    .pipe(
                        tap(filesData => filesData ? this.openFileProgressDialog(filesData) : '')
                    );
                apiUploadAfterClosed.subscribe();
            }
        } else if (this.tilecategory === 'API') {
            const apiUploadAfterClosed = this.dialogService.open(UtilityApiuploadComponent,
                {
                    data: {
                        clientUID: this.paramData.clientUID,
                        deliveryConstructUID: this.deliveryConstructUID,
                        userId: this.userId,
                        category: this.category
                        // fromApp: this.fromApp
                    }
                }).afterClosed
                .pipe(
                    tap(filesData => filesData ? this.openFileProgressDialog(filesData) : '')
                );
            apiUploadAfterClosed.subscribe();
        }
    }

    openFileProgressDialog(filesData) {
        if (filesData[0]['payloadfor'] !== 'API') {
            const totalSourceCount = filesData[0].sourceTotalLength;
            if (totalSourceCount > 0) {
                this.upload(filesData);
            }
        } else {
            this.uploadapi(filesData);
        }
    }


    fetchdatasetdetails() {
        this.problemStatementService.getDatasetdetails(this.clientUId, this.deliveryConstructUID, this.userId, this.tilecategory)
            .subscribe(data => {
                this.datasets = data;
                this.datasets.forEach(element => {
                    if (this.tilecategory !== 'API') {
                        if (element.SourceDetails === null || element.SourceDetails.FileDetail.length === 0) {
                            element.SourceDetails = 'NA';
                        } else {
                            element.SourceDetails = element.SourceDetails.FileDetail[0]['FileName'];
                        }
                    } else {
                        if (element.SourceDetails.ExternalAPIDetail !== null) {
                            element.AuthType = element.SourceDetails.ExternalAPIDetail.AuthType;
                            element.SourceDetails = element.SourceDetails.ExternalAPIDetail.Url;
                        } else {
                            element.SourceDetails = 'NA';
                        }
                    }
                    if (element.Status === 'P' || element.Status === 'I') {
                        let lastdataset = this.datasets[this.datasets.length - 1];
                        if (lastdataset.DataSetUId === element.DataSetUId) {
                            if (element.Status === 'P') {
                                this.retry();
                            }
                        }
                        element.Status = 'In Progress (' + element.Progress + ')';

                    } else if (element.Status === 'C') {
                        element.Status = 'Completed';
                        let lastdataset = this.datasets[this.datasets.length - 1];
                        if (lastdataset.DataSetUId === element.DataSetUId) {
                            if (element.Status === 'Completed') {
                                if (sessionStorage.getItem('progress') === 'Inprocess') {
                                    sessionStorage.setItem('progress', 'Completed');
                                }
                            }
                        }
                    } else {
                        element.Status = 'Error';
                        let lastdataset = this.datasets[this.datasets.length - 1];
                        if (lastdataset.DataSetUId === element.DataSetUId) {
                            if (element.Status === 'Error') {
                                if (sessionStorage.getItem('progress') === 'Inprocess') {
                                    sessionStorage.setItem('progress', 'Completed');
                                }
                            }
                        }
                    }
                });
                this.tabledatasets = this.datasets;
            });
    }


    uploadapi(filesData) {
        this.problemStatementService.sendapipayload(filesData[0]).subscribe(
            data => {
                if (data["Status"] === 'I') {
                    // if(this.datasetid === '' || this.datasetid === null) {
                    //   if(data.DataSetUId !== undefined) {
                    // this.datasetid = data.DataSetUId;}}   
                    this.timerSubscripton = timer(2000).subscribe(() => this.fetchdatasetdetails());
                    return this.timerSubscripton;
                } else if (data["Status"] === 'P') {
                    // this.datasetid = '';         
                    this.timerSubscripton = timer(2000).subscribe(() => this.fetchdatasetdetails());
                    return this.timerSubscripton;
                } else if (data["Status"] === 'E') {
                    this.progresscheck();
                }
                else if (data["Status"] === 'C') {
                    this.progresscheck();
                }
            }, error => {
                if (error.error.hasOwnProperty('Category')) {
                    this._notificationService.error(error.error.Category.Message);
                } else if (error.status === 500) {
                    this._notificationService.error(error.error);
                } else {
                    this._notificationService.error('Something went wrong.');
                }
            });
    }


    upload(filesData) {
        let requestParams;
        //const parentFileName = filesData[0].parentFileName;
        const dataSource = filesData[0].source;
        const category = filesData[0].category;
        const datasetname = filesData[0].datasetname;
        const IsPrivate = filesData[0].IsPrivate;
        const Encryption = filesData[0].Encryption;
        this._appUtilsService.getParamData().subscribe(data => this.paramData = data);

        if (this.datasetid === undefined) {
            // this._appUtilsService.loadingStarted();
            this.datasetid = '';
        }

        requestParams = {
            'DataSetUId': this.datasetid,
            'DatasetName': datasetname,
            'IsPrivate': IsPrivate,
            'ClientUId': this.paramData.clientUID,
            'DeliveryConstructUId': this.paramData.deliveryConstructUId,
            'UserId': this._appUtilsService.getCookies().UserId,
            'Category': 'null',
            'EncryptionRequired': Encryption,
            'SourceName': 'File'
        };
        //this.dataForMapping = requestParams;
        this.filesdataval = filesData;

        this.problemStatementService.uploadFileschunks(filesData, requestParams)
            .subscribe(
                data => {
                    if (data.body) {
                        if (data.body.Status) {
                            if (data.body.Status === 'I') {
                                this.fetchdatasetdetails();
                                // this._appUtilsService.loadingEnded();
                                if (this.datasetid === '' || this.datasetid === null) {
                                    this.datasetid = data.body.DataSetUId;
                                }
                                if (sessionStorage.getItem('progress') === 'Inprocess') {
                                    this.timerSubscripton = timer(2000).subscribe(() => this.upload(filesData));
                                    return this.timerSubscripton;
                                }
                                else {
                                    this.datasetid = null;
                                }
                            } else if (data.body.Status === 'P') {
                                sessionStorage.setItem('progress', 'Inprocess');
                                this.datasetid = '';
                                this.timerSubscripton = timer(2000).subscribe(() => this.fetchdatasetdetails());
                                return this.timerSubscripton;
                            } else if (data.body.Status === 'E') {
                                this.progresscheck();
                            }
                            else if (data.body.Status === 'C') {
                                this.progresscheck();
                            }
                        }
                    }
                }, error => {
                    if (error.error.hasOwnProperty('Category')) {
                        this._notificationService.error(error.error.Category.Message);
                    } else if (error.status === 500) {
                        if (error.error !== 'Object reference not set to an instance of an object.') {
                            this._notificationService.error(error.error);
                        }
                    } else {
                        this._notificationService.error('Something went wrong.');
                    }
                });

    }

    progresscheck() {
        if (this.tilecategory !== 'API') {
            sessionStorage.setItem('progress', 'Completed');
        }
        this.datasetid = '';
        this.getDatasetdetails();
        this.unsubscribe();
    }

    unsubscribe() {
        if (!this.coreUtilsService.isNil(this.timerSubscripton)) {
            this.timerSubscripton.unsubscribe();
        }
    }


    navigateToUseCaseDefinition(correlationId) {
        this.correlationId = correlationId;
        this.ls.setLocalStorageData('correlationId', correlationId);
        this.router.navigate(['/dashboard/problemstatement/utilities'],
            {
                queryParams: {
                    'modelCategory': '',
                    'displayUploadandDataSourceBlock': false,
                    'modelName': this.modelName
                }
            });
    }

    deleteDataset(DataSetUId, DatasetName) {
        const openModelDeletionPopup = this.dialogService.open(DatasetDeletionPopupComponent,
            { data: { dataSetUId: DataSetUId, DatasetName: DatasetName } })
            .afterClosed.pipe(
                tap(isDatasetFlag => isDatasetFlag ? this.deleteDatasetbydatasetid(DataSetUId) : '')
            );
        openModelDeletionPopup.subscribe();
    }

    deleteDatasetbydatasetid(DataSetUId: string) {
        sessionStorage.removeItem('progress');
        this.unsubscribe();
        var userid = this._appUtilsService.getCookies().UserId;
        if (!this.coreUtilsService.isNil(DataSetUId)) {
            this._appUtilsService.loadingStarted();
            this.problemStatementService.deletedataset(DataSetUId, userid).subscribe(
                data => {
                    this._notificationService.success('DataSet has been Deleted');
                    this._appUtilsService.loadingEnded();
                    this.getDatasetdetails();
                }, error => {
                    this._notificationService.error(error.error);
                    this._appUtilsService.loadingEnded();
                });
        }
    }

    onClickCategory(category) {
        this.tilecategory = category;
        this.page = 1;
        this.getDatasetdetails();
        return false;
    }

    sidebartoggle() {
        this.issidebartoggle = !this.issidebartoggle;
    }

    viewDataset(dataSetUId) {
        this._appUtilsService.loadingStarted();
        this.problemStatementService.showdatasetdetais(dataSetUId, this.decimalPoint).subscribe(data => {
            if (data) {
                this.datasetdata = JSON.parse(data);
                if (this.datasetdata.length > 0) {
                    if (this.datasetdata !== []) {
                        this.datasettableData = this.datasetdata;
                        this.datasetColumnsList = Object.keys(this.datasetdata[0]);
                        this._appUtilsService.loadingEnded();
                        this.dialogService.open(ViewDatasetrecordsComponent, {
                            data: {
                                'Datasettable': this.datasettableData,
                                'Datasetcolumnlist': this.datasetColumnsList
                            }
                        });
                    } else {
                        this._appUtilsService.loadingEnded();
                    }
                } else {
                    this._appUtilsService.loadingEnded();
                }
            }
        }, error => {
            this._appUtilsService.loadingEnded();
            this._notificationService.error('Something went wrong to get View Upload Data.');
        });
        return false;
    }

    iconsPinUnpin() {
        this.pinUnpinIconsBar = !this.pinUnpinIconsBar;
        return false;
    }

    searchDataset(searchText) {
        this.totalRec = this.totalRecords;
        this.tabledatasets = this.datasets;
        if (!this.coreUtilsService.isNil(searchText)) {

            const statusc = 'Completed';
            const statusp = 'Pending';
            const statusi = 'In Progress';
            const statuse = 'Error';

            this.status = '';

            if (!this.coreUtilsService.isNumeric(searchText) && statusc.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
                this.status = 'Completed';
            }
            if (!this.coreUtilsService.isNumeric(searchText) && statusp.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
                this.status = 'Pending';
            }
            if (!this.coreUtilsService.isNumeric(searchText) && statusi.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
                this.status = 'In Progress';
            }
            if (!this.coreUtilsService.isNumeric(searchText) && statuse.toLowerCase().indexOf(searchText.toLowerCase()) > -1) {
                this.status = 'Error';
            }

            this.tabledatasets = this.datasets.filter((item) => {
                return (!this.coreUtilsService.isNil(item.DataSetName) && item.DataSetName.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                    || (!this.coreUtilsService.isNil(item.CreatedOn) && item.CreatedOn.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                    || (!this.coreUtilsService.isNil(item.RecordCount) && item.RecordCount.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                    || (!this.coreUtilsService.isNil(item.Progress) && item.Progress.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                    || (!this.coreUtilsService.isNil(item.DataSetUId) && item.DataSetUId.toLowerCase().indexOf(searchText.toLowerCase()) > -1)
                    || (!this.coreUtilsService.isNil(this.status) && item.Status.toString().indexOf(this.status) > -1)
                    || (!this.coreUtilsService.isNil(item.Status) && item.Status.toString().indexOf(searchText) > -1);
            });
            this.totalRec = this.tabledatasets.length;
        }
    }

    toggleNavBarLabels() {
        this.isNavBarLabelsToggle = !this.isNavBarLabelsToggle;
        return false;
    }

    closeSearchBox() {
        this.searchText = '';
        this.tabledatasets = this.datasets;
    }

}

