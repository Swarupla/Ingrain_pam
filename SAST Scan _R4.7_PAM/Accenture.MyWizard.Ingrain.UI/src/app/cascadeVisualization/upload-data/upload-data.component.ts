import { HttpClient } from '@angular/common/http';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, throwError, timer } from 'rxjs';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ExcelService } from 'src/app/_services/excel.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CascadeVisualizationService } from '../services/cascade-visualization.service';
import { NotificationData } from 'src/app/_services/usernotification';
import { ApiService } from 'src/app/_services/api.service';
import * as $ from 'jquery';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { AuthenticationService } from 'src/app/msal-authentication/msal-authentication.service';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
declare var userNotification: any;


@Component({
    selector: 'app-upload-data',
    templateUrl: './upload-data.component.html',
    styleUrls: ['./upload-data.component.scss']
})
export class UploadDataComponent implements OnInit {

    isnavBarToggle: boolean;

    constructor(private cascadeVisService: CascadeVisualizationService, private ns: NotificationService,
        private _excelService: ExcelService, private appUtilsService: AppUtilsService, private route: ActivatedRoute,
        private router: Router, private api: ApiService, private envService: EnvironmentService, private msalAuthentication: AuthenticationService
        , private dialogService: DialogService) {
        this.route.queryParams.subscribe(params => {
            this.cascadedId = params['CascadedId'];
            this.category = params['Category'];
            this.modelName = params['ModelName'];
            this.dcid = params['DeliveryConstructUId'];
            this.clientId = params['ClientUId'];
            this.cascadeVisService.cascadeVisData.cascadedId = this.cascadedId;
            this.cascadeVisService.cascadeVisData.categoryType = this.category;
            this.cascadeVisService.cascadeVisData.deliveryConstructUId = this.dcid;
            this.cascadeVisService.cascadeVisData.clientUID = this.clientId;
            this.cascadeVisService.cascadeVisData.modelName = this.modelName;
        });
    }

    cascadedId = '';
    uniqueId;
    sourceTotalLength = 0;
    allowedExtensions = ['.xlsx', '.csv'];
    files = [];
    entityArray = [];
    uploadFilesCount = 0;
    agileFlag: boolean;
    modelName;
    subscription: any;
    paramData: any;
    dataforDownload;
    view = 'file';
    category;
    dcid;
    clientId;
    dataUploadValidationMessage;
    isUploadView = false;
    timerSubscripton: Subscription;
    instanceType: string;
    env: string;

    infoMessage = `<div>
  <b>Export File Password Hint:</b><br>
  &nbsp;&nbsp;&nbsp;&nbspFirst four digits of account id followed by First character of First Name in upper case followed by First character of Last Name in small case.<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 1:</b> Account id having four or more digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 12345678 and   your Name is - John Doe then the password would be 1234Jd<br>
  &nbsp;&nbsp;&nbsp;&nbsp <b>Example 2:</b> Account id having less than 4 digits:<br>
  &nbsp;&nbsp;&nbsp;&nbspIf your account id is 57 and   your Name is - John Doe then the password would be 0057Jd<br>
  &nbsp;&nbsp;&nbsp;&nbspClick on <img src="assets/images/user.svg" width="12" height="12"> top right corner of the screen to see your details like name (Last Name, First Name) and account id. Click on <img src="assets/images/icon-myw-eye.svg" width="12" height="12"> to view the account id.
  </div>`;

    ngOnInit() {
        this.instanceType = sessionStorage.getItem('Instance');
        this.env = sessionStorage.getItem('Environment');
        if (this.envService.environment.authProvider.toLowerCase() === 'AzureAD'.toLowerCase()) {
            if (!this.msalAuthentication.msalService.instance.getAllAccounts().length) {
                this.msalAuthentication.login();
            } else {
                this.msalAuthentication.getToken().subscribe(data => {
                    if (data) {
                        this.envService.setMsalToken(data);
                        this.appUtilsService.loadingImmediateEnded();
                        this.checkPageReload();
                        this.getInfluencers();
                    }
                });
            }
        } else if (this.envService.environment.authProvider.toLowerCase() === 'Local'.toLowerCase()  || this.envService.environment.authProvider.toLowerCase() === 'Form'.toLowerCase()) {
            if (this.getToken()) {
                this.appUtilsService.loadingImmediateEnded();
                this.checkPageReload();
                this.getInfluencers();
            } else {
                this.timerSubscripton = timer(1000, 3000).subscribe(() => {
                    if (this.getToken()) {
                        this.appUtilsService.loadingImmediateEnded();
                        this.checkPageReload();
                        this.getInfluencers();
                    }
                });
            }
        }
    }

    checkPageReload() {
        const isLoadedBefore = sessionStorage.getItem("IsCascadeLoadedBefore");
        if (isLoadedBefore == "true") {
            return;
        }
        else {
            sessionStorage.setItem("IsCascadeLoadedBefore", 'true');
            window.location.reload();
        }
    }

    getToken() {
        return localStorage.getItem('headerAuthToken');
    }

    toggleNavBar() {
        this.isnavBarToggle = !this.isnavBarToggle;
        return false;
    }

    getInfluencers() {
        this.appUtilsService.loadingStarted();
        if (this.timerSubscripton) {
            this.timerSubscripton.unsubscribe();
        }
        this.cascadeVisService.getCascadeInfluencers(this.cascadedId)
            .subscribe(colData => {
                this.dataforDownload = colData;
                this.cascadeVisService.cascadeVisData.dataForDownload = this.dataforDownload;
                this.cascadeVisService.cascadeVisData.isOnlyFileupload = colData['IsonlyFileupload'];
                this.cascadeVisService.cascadeVisData.isBoth = colData['IsBoth'];
                this.cascadeVisService.cascadeVisData.isonlySingleEntity = colData['IsonlySingleEntity'];
                this.cascadeVisService.cascadeVisData.isMultipleEntities = colData['IsMultipleEntities'];
                if (colData['IsonlyFileupload'] === true) {
                    this.dataUploadValidationMessage = 'Sub-models are created using File upload. Please select appropriate data source.';
                } else if (colData['IsBoth'] === true) {
                    this.dataUploadValidationMessage = 'Few Sub-models are created using File. Please select appropriate data source.';
                }
                this.cascadeVisService.cascadeVisData.dataUploadValidationMessage = this.dataUploadValidationMessage;
                if (colData['IsVisualizationAvaialble'] === true) {
                    this.isUploadView = false;
                    this.uniqueId = colData['UniqueId'];
                    this.cascadeVisService.cascadeVisData.uniqueId = this.uniqueId;
                    this.appUtilsService.loadingEnded();
                    this.router.navigate(['/visualizationGraph'], { queryParams: { CascadedId: this.cascadedId, UniqueId: this.uniqueId, ModelName: this.modelName, DeliveryConstructUId: this.dcid, ClientUId: this.clientId } });
                } else if (colData['IsMultipleEntities'] === true || colData['IsonlySingleEntity'] === true) {
                    this.isUploadView = false;
                    this.view = 'entity';
                    this.uniqueId = colData['UniqueId'];
                    this.cascadeVisService.cascadeVisData.uniqueId = this.uniqueId;
                    this.uploadFile();
                    //  this.router.navigate(['/visualizationGraph'], { queryParams: {CascadedId: this.cascadedId, UniqueId: this.uniqueId, ModelName: this.modelName, DeliveryConstructUId: this.dcid, ClientUId: this.clientId} });
                } else {
                    this.appUtilsService.loadingEnded();
                    this.isUploadView = true;
                }
            }, error => {
                this.appUtilsService.loadingEnded();
                throwError(error);
            });
    }

    downloadData() {
        const self = this;
        if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
            this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
                if (confirmationflag === true) {
                    if (self.dataforDownload) {
                        self.ns.success('Your Data will be downloaded shortly');
                        self.dataforDownload = self.dataforDownload['InputSample'] || [];
                        self._excelService.exportAsExcelFile(self.dataforDownload, 'TemplateDownloaded');
                    }
                }
            });
        } else {
            userNotification.showUserNotificationModalPopup();
            $(".notification-button-close").click(function () {
                if (self.dataforDownload) {
                    self.ns.success('Your Data will be downloaded shortly');
                    self.dataforDownload = self.dataforDownload['InputSample'] || [];
                    self._excelService.exportAsPasswordProtectedExcelFile(self.dataforDownload, 'TemplateDownloaded').subscribe(response => {
                        self.ns.warning('Downloaded file is password protected, click on the Info icon to view the password hint');
                        let binaryData = [];
                        binaryData.push(response);
                        let downloadLink = document.createElement('a');
                        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
                        downloadLink.setAttribute('download', 'TemplateDownloaded' + '.zip');
                        document.body.appendChild(downloadLink);
                        downloadLink.click();
                    }, (error) => {
                        self.ns.error(error);
                    });
                }
            });
        }
    }

    uploadView(value) {
        if (value === 'entity') {
            if (this.dataUploadValidationMessage) {
                this.ns.error(this.dataUploadValidationMessage);
            } else {
                this.view = value;
            }
        } else {
            this.view = value;
        }
    }

    getFileDetails(e) {
        if (this.agileFlag === true) {
            this.uploadFilesCount = e.target.files.length;
        } else {
            this.uploadFilesCount += e.target.files.length;
        }
        let validFileExtensionFlag = true;
        let validFileNameFlag = true;
        let validFileSize = true;
        const resourceCount = e.target.files.length;
        if (this.sourceTotalLength < 5 && resourceCount <= (5 - this.sourceTotalLength)) {
            const files = e.target.files;
            let fileSize = 0;
            for (let i = 0; i < e.target.files.length; i++) {
                const fileName = files[i].name;
                const dots = fileName.split('.');
                const fileType = '.' + dots[dots.length - 1];
                if (!fileName) {
                    validFileNameFlag = false;
                    break;
                }
                if (this.allowedExtensions.indexOf(fileType) !== -1) {
                    fileSize = fileSize + e.target.files[i].size;
                    const index = this.files.findIndex(x => (x.name === e.target.files[i].name));
                    validFileNameFlag = true;
                    validFileExtensionFlag = true;
                    if (index < 0) {
                        if (this.agileFlag === true) {
                            this.files = [];
                            this.files.push(e.target.files[0]);
                        } else {
                            this.files.push(e.target.files[i]);
                        }
                        this.uploadFilesCount = this.files.length;
                        this.sourceTotalLength = this.files.length + this.entityArray.length;
                    }
                } else {
                    validFileExtensionFlag = false;
                    break;
                }
            }
            if (fileSize <= 136356582) {
                validFileSize = true;
            } else {
                validFileSize = false;
                this.files = [];
                this.uploadFilesCount = this.files.length;
                this.sourceTotalLength = this.files.length + this.entityArray.length;
            }
            if (validFileNameFlag === false) {
                this.ns.error('Kindly upload a file with valid name.');
            }
            if (validFileExtensionFlag === false) {
                this.ns.error('Kindly upload .xlsx or .csv file.');
            }
            if (validFileSize === false) {
                this.ns.error('Kindly upload file of size less than 130MB.');
            }
        } else {
            this.uploadFilesCount = this.files.length;
            this.ns.error('Maximum 5 sources of data allowed.');
        }
    }

    uploadFile() {
        console.log('busycount', this.appUtilsService.busyCount);
        console.log('spinner', this.appUtilsService.spinnerVisible);
        this.appUtilsService.loadingStarted();
        let isFileUpload = true;
        if (this.view === 'entity') {
            isFileUpload = false;
        }
        const params = {
            'UserId': this.appUtilsService.getCookies().UserId,
            'IsFileUpload': isFileUpload,
            'CascadedId': this.cascadedId,
            'ModelName': this.modelName,
            'ClientUID': this.clientId,
            'DCUID': this.dcid
        };
        this.cascadeVisService.uploadData(this.files, params).subscribe(res => {
            if (res.body !== '') {
                this.appUtilsService.loadingEnded();
                if (res.body['Status'] === 'C') {
                    if (res.body['ValidatonMessage'] !== null) {
                        this.ns.error(res.body['ValidatonMessage']);
                    } else {
                        if (res.body['IsUploaded'] === true) {
                            if (this.view === 'entity') {
                                this.ns.success('Data Processed Successfully.');
                            } else {
                                this.ns.success('File Uploaded Successfully.');
                            }
                            this.cascadedId = res.body['CascadedId'];
                            this.uniqueId = res.body['UniqueId'];
                            this.router.navigate(['/visualizationGraph'], { queryParams: { CascadedId: this.cascadedId, UniqueId: this.uniqueId, ModelName: this.modelName, DeliveryConstructUId: this.dcid, ClientUId: this.clientId } });
                        } else {
                            this.isUploadView = true;
                            this.ns.error(res.body['ErrorMessage']);
                        }
                    }
                } else if ((res.body['Status'] === 'E')) {
                    this.isUploadView = true;
                    this.ns.error(res.body['ErrorMessage']);
                }
            }
        }, error => {
            console.log('busycount', this.appUtilsService.busyCount);
            console.log('spinner', this.appUtilsService.spinnerVisible);
            this.appUtilsService.loadingEnded();
            this.isUploadView = true;
            this.ns.error('Something went wrong.');
        });
    }

    allowDrop(event) {
        event.preventDefault();
    }

    onDrop(event) {
        this.files = [];
        event.preventDefault();
        for (let i = 0; i < event.dataTransfer.files.length; i++) {
            this.files.push(event.dataTransfer.files[i]);
        }
    }

}
