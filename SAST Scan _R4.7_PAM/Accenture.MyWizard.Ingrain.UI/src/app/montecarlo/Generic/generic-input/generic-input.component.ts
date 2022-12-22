import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ApiCore } from '../../services/api-core.service';
import { GridDataService } from '../../ADSP/grid-table/grid-data.service';

@Component({
  selector: 'app-generic-input',
  templateUrl: './generic-input.component.html',
  styleUrls: ['./generic-input.component.scss']
})
export class GenericInputComponent implements OnInit {
  public templateId;
  public gridDataFeatures;
  public gridTableRefresh = new BehaviorSubject<any>('');
  constructor(private apiCore: ApiCore, public gridtable: GridDataService) { }

  ngOnInit() {
    // this.setGridInputTable(GENERIC_GRID); For Static payload
    this.templateId = this.apiCore.paramData.selectInputId;
  }

  public setGridInputTable(data) {
    if (data.hasOwnProperty('TemplateInfo')) {
      this.gridDataFeatures = data.TemplateInfo;
    } else {
      this.gridDataFeatures = data;
    }
    this.gridtable.genericRowData.row = {};
    this.gridtable.genericRowData.column = [];
    // this.gridDataFeatures = GENERIC_GRID; For Static payload
    const clonedData = JSON.parse(JSON.stringify(this.gridDataFeatures));
    this.gridTableRefresh.next(clonedData);
  }

  addRowGenericEmit() {
  
  }

}
