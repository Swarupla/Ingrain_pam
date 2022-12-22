import { Component, OnInit } from '@angular/core';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CascadeVisualizationService } from '../services/cascade-visualization.service';
import { ExcelService } from 'src/app/_services/excel.service';
import { NotificationData } from 'src/app/_services/usernotification';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';
import { ApiService } from 'src/app/_services/api.service';
import * as $ from 'jquery';
import { UserNotificationpopupComponent } from 'src/app/components/user-notificationpopup/user-notificationpopup.component';
import { DialogService } from 'src/app/dialog/dialog.service';
import { BsModalRef} from 'ngx-bootstrap/modal'
declare var userNotification: any;

@Component({
  selector: 'app-show-data',
  templateUrl: './show-data.component.html',
  styleUrls: ['./show-data.component.scss']
})
export class ShowCascadeDataComponent implements OnInit {

  constructor(public bsmodelRef : BsModalRef,
    private cascadeVisService: CascadeVisualizationService, private ns: NotificationService,
    private appUtilsService: AppUtilsService, private _excelService: ExcelService, private dialogService :DialogService, private api: ApiService) { }

  cascadedId;
  uniqueId;
  tableData = [];
  page = 1;
  isLoading = true;
  dataToDownload = [];
  instanceType : string;
  env : string;

  ngOnInit() {
    this.instanceType = sessionStorage.getItem('Instance');
    this.env = sessionStorage.getItem('Environment');
    this.api.openDisclaimer();
    this.cascadedId = this.cascadedId;
    this.uniqueId = this.uniqueId;
    this.viewData();
  }

  viewData() {
    this.appUtilsService.loadingStarted();
    this.cascadeVisService.showCascadeData(this.cascadedId, this.uniqueId).subscribe(data => {
      this.appUtilsService.loadingEnded();
      this.isLoading = false;
      if (data.Data) {
        this.dataToDownload = data.Data;
        this.tableData = [];
      // Code for keeping the sequence intact
        if (data.Data.length > 0) {
          data.Data.forEach(element => {
            let arr = [];
            Object.entries(element).forEach(
              ([key, value]) => {
                const el = new Map();
                el.set(key, value);
                arr.push(el);
              });
              this.tableData.push(arr);
          });
        }
      } else {
        this.ns.error('Something went wrong.');
      }
    }, error => {
      this.appUtilsService.loadingEnded();
      this.ns.error('Something went wrong.');
    });
    /* let data = {
      "CascadedId": "b5b88cd4-11d1-4b62-8f5d-e916efc61860",
      "UniqueId": "10e80d28-e5bd-45a5-9797-70f8a7c9f283",
      "IsException": false,
      "ErrorMessage": null,
      "Data": [
          {
              "ProblemTickets": 13001,
              "ReleaseDate": "2018-08-01T00:00:00Z",
              "TicketsCount": 5,
              "AvgPlannedHours": 38.6,
              "AvgRequestedHours": 53.6,
              "IncidentTickets": 35,
              "OutboundInterfaces": 50,
              "EmergencyTickets": 44
          },
          {
              "ProblemTickets": 13002,
              "ReleaseDate": "2018-08-03T17:00:00Z",
              "TicketsCount": 7,
              "AvgPlannedHours": 40.14,
              "AvgRequestedHours": 54.28,
              "IncidentTickets": 49,
              "OutboundInterfaces": 61,
              "EmergencyTickets": 48
          },
          {
              "ProblemTickets": 13003,
              "ReleaseDate": "2018-08-05T13:00:00Z",
              "TicketsCount": 8,
              "AvgPlannedHours": 41.375,
              "AvgRequestedHours": 54.875,
              "IncidentTickets": 27,
              "OutboundInterfaces": 5,
              "EmergencyTickets": 15
          },
          {
              "ProblemTickets": 13004,
              "ReleaseDate": "2018-08-06T13:00:00Z",
              "TicketsCount": 9,
              "AvgPlannedHours": 55.11,
              "AvgRequestedHours": 68.78,
              "IncidentTickets": 14,
              "OutboundInterfaces": 7,
              "EmergencyTickets": 8
          },
          {
              "ProblemTickets": 13005,
              "ReleaseDate": "2018-08-07T13:00:00Z",
              "TicketsCount": 5,
              "AvgPlannedHours": 65.2,
              "AvgRequestedHours": 75.2,
              "IncidentTickets": 21,
              "OutboundInterfaces": 6,
              "EmergencyTickets": 12
          },
          {
              "ProblemTickets": 13006,
              "ReleaseDate": "2018-08-08T13:00:00Z",
              "TicketsCount": 1,
              "AvgPlannedHours": 72.0,
              "AvgRequestedHours": 80.0,
              "IncidentTickets": 38,
              "OutboundInterfaces": 9,
              "EmergencyTickets": 11
          },
          {
              "ProblemTickets": 13007,
              "ReleaseDate": "2018-08-09T13:00:00Z",
              "TicketsCount": 9,
              "AvgPlannedHours": 42.0,
              "AvgRequestedHours": 49.33,
              "IncidentTickets": 8,
              "OutboundInterfaces": 15,
              "EmergencyTickets": 7
          },
          {
              "ProblemTickets": 13008,
              "ReleaseDate": "2018-08-01T00:00:00Z",
              "TicketsCount": 5,
              "AvgPlannedHours": 38.6,
              "AvgRequestedHours": 53.6,
              "IncidentTickets": 35,
              "OutboundInterfaces": 50,
              "EmergencyTickets": 44
          },
          {
              "ProblemTickets": 13009,
              "ReleaseDate": "2018-08-03T17:00:00Z",
              "TicketsCount": 7,
              "AvgPlannedHours": 40.14,
             "AvgRequestedHours": 54.28,
              "IncidentTickets": 49,
              "OutboundInterfaces": 61,
              "EmergencyTickets": 48
          },
          {
              "ProblemTickets": 13010,
              "ReleaseDate": "2018-08-05T13:00:00Z",
              "TicketsCount": 8,
              "AvgPlannedHours": 41.375,
              "AvgRequestedHours": 54.875,
              "IncidentTickets": 27,
              "OutboundInterfaces": 5,
              "EmergencyTickets": 15
          }
      ]
  }; */
  // this.tableData = data.Data;

  }

  onClose() {
    this.bsmodelRef.hide();
  }


  downloadData() {
    const self = this;
    if (this.instanceType === 'PAM' || this.env === 'FDS' || this.env === 'PAM') {
      this.dialogService.open(UserNotificationpopupComponent, {}).afterClosed.subscribe(confirmationflag => {
        if (confirmationflag === true) {
          if (self.tableData) {
            self.ns.success('Your Data will be downloaded shortly');
            self._excelService.exportAsExcelFile(self.dataToDownload, 'TemplateDownloaded');
          }
         }
      });
    } else {
      userNotification.showUserNotificationModalPopup();
      $(".notification-button-close").click(function () {
        if (self.tableData) {
          self.ns.success('Your Data will be downloaded shortly');
          self._excelService.exportAsPasswordProtectedExcelFile(self.dataToDownload, 'TemplateDownloaded').subscribe(response => {
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
}
