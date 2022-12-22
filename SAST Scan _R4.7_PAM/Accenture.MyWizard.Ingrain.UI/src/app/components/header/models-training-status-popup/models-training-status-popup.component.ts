import { Component, OnInit, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { interval, timer, Observable, Subscription, throwError, of, EMPTY } from 'rxjs';
import { DataEngineeringService } from 'src/app/_services/data-engineering.service';
import { catchError, tap, switchMap, finalize } from 'rxjs/operators';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { DialogService } from 'src/app/dialog/dialog.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';
import { StatusPopupComponent } from 'src/app/components/status-popup/status-popup.component';
import { ApiService } from 'src/app/_services/api.service';
import { EnvironmentService } from 'src/app/_services/EnvironmentService';

@Component({
  selector: 'app-models-training-status-popup',
  templateUrl: './models-training-status-popup.component.html',
  styleUrls: ['./models-training-status-popup.component.scss']
})

export class ModelsTrainingStatusPopupComponent implements OnInit {

  @ViewChild('helppanelopen', { static: false }) helppanelopen;
  @ViewChild('helppanelclose', { static: false }) helppanelclose;
  @ViewChild('selectedDataType', { static: false }) selectedDataType;


  page = 1;
  status = false;
  cleanupData = false;
  counter = 0;
  count = 0;
  pageInfo = 'DataCleanUp';
  featureNames;
  featureNameArray = [];
  showhelppanel = false;

  isLoading = false;
  showDataColumnsList: any[];
  tableData: any[];
  payLoadData = [];
  subscription: any;
  paramData: any;
  modelTrainingStatusText: string;
  disableTrainBtn: boolean = true;
  checkBoxArr = [];
  selectAllChecked: boolean = false;
  minPageSelect = 0;
  maxPageSelect = 0;

  constructor(private des: DataEngineeringService, private ns: NotificationService,
    private dr: DialogRef, private dc: DialogConfig, private dialogService: DialogService, private coreUtilsService: CoreUtilsService, 
    private appUtilsService: AppUtilsService, private probStatementService: ProblemStatementService, private api: ApiService, private envService: EnvironmentService ) {
  }

  ngOnInit() {
    this.tableData = this.dc.data;

    this.subscription = this.appUtilsService.getParamData().subscribe(paramData => {
      this.paramData = paramData;
    });

    this.modelTrainingStatusText = this.envService.environment.modelTrainingStatusText;

    // this.api.getJSON().subscribe(data => {
    //   this.modelTrainingStatusText = data['modelTrainingStatusText'];
    // });
  }

  ngOnDestroy() {
    this.tableData = [];
    this.dc.data = [];
    this.checkBoxArr = [];
    this.subscription.unsubscribe();
  }

  openHelper(rowData) {
    this.showhelppanel = true;
  }

  closeHelper() {
    this.showhelppanel = false;
    this.helppanelopen.nativeElement.classList.remove('show');
  }

  ignore() {
    this.status = false;
  }

  onClose() {
    this.dr.close();
  }

  onTrainModel() {
    this.tableData.map(rowData => {
      if(rowData.Checked){
        this.payLoadData.push({
          "ModelName": rowData.ModelName,
          "ApplicationId": rowData.ApplicationID,
          "ClientID": rowData.ClientId == null ? this.paramData.clientUID : rowData.ClientId,
          "DCID": rowData.DeliveryconstructId == null ? this.paramData.deliveryConstructUId : rowData.DeliveryconstructId,
          "UsecaseID": rowData.UsecaseID,
          "CorrelationId": rowData.CorrelationId,
          "UserId": this.appUtilsService.getCookies().UserId,
          "ApplicationName": rowData.ApplicationName,
          "Status": rowData.Status,
          "IsCascadeModelTemplate": rowData.IsCascadeModelTemplate
        })
      }
    });

    this.probStatementService.postModelReTraning(this.payLoadData).subscribe(data => {
      if (data) {
        this.onClose();
        this.openStatusPopup(data.Message);
      }
    });
  }

  openStatusPopup (msg) {
    this.dialogService.open(StatusPopupComponent, {
      data: {
        'privateModelTrainingMsg': msg,
        'modelTrainingStatusText': this.modelTrainingStatusText
      }
    }).afterClosed.subscribe(statuspopupdata => {
      if (statuspopupdata === true) {
      }
    });
  }

  selectData () {
    this.checkBoxArr = [];
    this.tableData.map(rowData => {
      if (rowData.Checked) {
        this.checkBoxArr.push(true);
      } else {
        this.checkBoxArr.push(false);
        this.selectAllChecked = false;
      }
    });

    if (this.checkBoxArr.indexOf(true) > -1) {
      this.disableTrainBtn = false;
    } else {
      this.disableTrainBtn = true;
    }
  }

  selectAll() {

    this.maxPageSelect = this.page * 10;
    this.minPageSelect = this.maxPageSelect - 10;

    this.checkBoxArr = [];
    if (this.selectAllChecked) {
      this.tableData.map((rowData, index) => {
        if (rowData['Status'] != 'C') {
          // if(index >= this.minPageSelect && index <= this.maxPageSelect)
          rowData.Checked = true;
          this.checkBoxArr.push(true);
        } else {
          rowData.Checked = false;
          this.checkBoxArr.push(false);
        }
      });
    } else {
      this.tableData.map(rowData => {
        rowData.Checked = false;
        this.checkBoxArr.push(false);
      });
    }

    if (this.checkBoxArr.indexOf(true) > -1) {
      this.disableTrainBtn = false;
    } else {
      this.disableTrainBtn = true;
    }
  }

}

