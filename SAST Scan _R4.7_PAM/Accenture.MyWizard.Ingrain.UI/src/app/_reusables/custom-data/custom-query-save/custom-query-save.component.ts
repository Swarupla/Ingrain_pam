import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BsModalService } from 'ngx-bootstrap/modal';
import { DateColumnComponent } from 'src/app/components/dashboard/problem-statement/pst-use-case-definition/date-column/date-column.component';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { CustomDataService } from 'src/app/_services/custom-data.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ModalNameDialogComponent } from '../../modal-name-dialog/modal-name-dialog.component';

@Component({
  selector: 'app-custom-query-save',
  templateUrl: './custom-query-save.component.html',
  styleUrls: ['./custom-query-save.component.scss']
})
export class CustomQuerySaveComponent implements OnInit, OnChanges {
  @Output() onSaveQuery = new EventEmitter<any>();
  @Input() modalNameRequired: boolean;
  @Input() previousQuery: string;
  @Input() isDisabled : boolean;
  @Input() serviceLevel : string;
  query: string;
  queryResponse = [];
  isQueryButtonEnabled: boolean = false;
  paramData: any;
  placeholder: string = 'Enter Query for Ex : {"aggregate":"collection_name","pipeline":[{"$match":{ }}], "cursor":{}}';

  tableData = [];
  showDataColumnsList = [];
  problemTypeFlag = false;
  columnHeaders: any[];
  page = 1;
  totalRec = 100;
  tabOptions: string[] = [];
  indexOfSelectedTab = '0';
  activeTab = '';
  clientUId: string;
  deliveryTypeName: string;
  userid: string;
  errorvalid: boolean;
  dateColumnsList = [];
  rows='1';
  
  constructor(private _bsModalService: BsModalService, private ns: NotificationService,
    private customDataService: CustomDataService, private appUtilsService: AppUtilsService) { }

  ngOnInit(): void {
    this.errorvalid = false;
    if (this.previousQuery) {
      this.query = this.previousQuery;
    }
    this.onKey(this.query);
    this.appUtilsService.getParamData().subscribe(paramData => {
      this.paramData = paramData;
      this.clientUId = paramData.clientUID;
      this.deliveryTypeName = paramData.deliveryConstructUId;
    });
    this.userid = this.appUtilsService.getCookies().UserId;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.previousQuery?.currentValue != '' && changes.previousQuery?.currentValue !== undefined) {
      this.query = changes.previousQuery.currentValue;
      this.rows = Math.ceil((this.query.length/100)).toString();
      this.onKey(this.query);
    }
  }

  submitQuery() {
    this.isQueryButtonEnabled = false;
    this.dateColumnsList = [];
    this.queryResponse = [];

    if (this.errorvalid) {
      this.ns.error("Please Enter valid Query and Try Again!!!...");
    } 
    // else if ((this.previousQuery) ? this.previousQuery.trim() === this.query.trim() : false) {
    //   this.ns.error("You have not modified the query.");
    // }
    else {
      let configData = {
        userId: this.userid,
        clientUID: this.clientUId,
        deliveryConstructUID: this.paramData.deliveryConstructUId,
        deliveryTypeName: this.deliveryTypeName
      }
      this.customDataService.getQueryResponse(configData, this.query, this.serviceLevel).subscribe(response => {
        this.appUtilsService.loadingEnded();
        this.verifyBatchSize(this.query);
        if (typeof response === 'string') {
          try {
            response = JSON.parse(response);
          } catch (e) {
            this.ns.error('Unable to display Data, Invalid Response.');
          }
        }
        this.queryResponse = response.Result? response.Result : [];
        this.isQueryButtonEnabled = (this.queryResponse.length > 0) ? true : false;
        response.DateColumns?.forEach(
          column=>{this.dateColumnsList.push(column.Name)
        });
        this.tableData = response.Result;
        this.showDataColumnsList = (this.queryResponse.length > 0) ? Object.keys(this.queryResponse[0]) : [];
        this.appUtilsService.loadingEnded();

        if (this.problemTypeFlag && this.tableData != undefined) {
          for (const key in this.tableData) {
            if (this.tableData.hasOwnProperty(key)) {
              if (this.tableData[key].length > 0) {
                this.tabOptions.push(key);
              }
            }
          }
          this.activeTab = this.tabOptions[0];
        } else {
          if (this.problemTypeFlag && this.tableData === undefined) {
            this.problemTypeFlag = false;
          }
          this.tableData = this.tableData;
        }
      }, error => {
        this.appUtilsService.loadingEnded();
        if (error.status === 500) {
          this.ns.error(error.error);
        } else {
          this.ns.error('Something went wrong while fetching Query response.');
        }
        this.queryResponse = [];
      });
    }

  }

  saveQuery() {
    this._bsModalService.show(DateColumnComponent, { class: 'modal-dialog modal-dialog-centered', backdrop: 'static', initialState: { title: 'Choose Date Column', dateColumnsList : this.dateColumnsList } }).content.Data.subscribe(selectedColumn => {
      if (selectedColumn) {
        if (this.modalNameRequired) {
          this._bsModalService.show(ModalNameDialogComponent, { class: 'modal-dialog modal-dialog-centered', backdrop: 'static', initialState: { title: 'Save View', placeholder: 'Enter model Name' } }).content.Data.subscribe(modelName => {
            this.onSaveQuery.emit({ modelName: modelName, query: this.query, DateColumn : selectedColumn, source : CustomDataTypes.Query });
          });
        }
        else {
          this.onSaveQuery.emit({ modelName: '', query: this.query, DateColumn : selectedColumn, source : CustomDataTypes.Query });
        }
      }
    });
  }

  onTabClick(i, tabName) {
    this.indexOfSelectedTab = i;
    this.activeTab = tabName;
  }

  setMyClasses(index) {

    const flag = (this.indexOfSelectedTab === index);
    return {
      'nav-link': true,
      active: flag,
      show: flag
    };
  }

  onKey(val: string) {
    if (val === undefined) {
      val = '';
    }
    if (val !== '') {
      this.errorvalid = true;
      this.errorvalid = val.includes('cursor') ? false : true;
    } else {
      this.errorvalid = true;
    }
  }
  textAreaAdjust(element) {
    element = element.target;
    this.rows = (element.value.split('\n').length + element.value.length/100).toString();
  }

  verifyBatchSize(query){
    if(!query.includes('"batchSize"') && !query.includes('batchSize')){
      this.ns.warning('BatchSize has not been provided in the Query only 101 records will be ingested by default.');
    }
  }
}
