import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { CustomDataTypes } from 'src/app/_enums/custom-data-types.enum';
import { serviceLevel } from 'src/app/_enums/service-types.enum';

@Component({
  selector: 'app-data-view',
  templateUrl: './custom-data-view.component.html',
  styleUrls: ['./custom-data-view.component.scss']
})
export class CustomDataViewComponent implements OnInit {
  @Input() title: string;
  @Input() selectedServiceName: string;
  @Input() ServiceId: string;
  @Output() Data = new EventEmitter<any>();

  isQueryViewEnabled: boolean = true;
  isApiViewEnabled: boolean;
  isFiltersViewEnabled: boolean;
  ApiConfig = {
    isDataSetNameRequired : false,
    isCategoryRequired : false,
    isIncrementalRequired : false
  };
  serviceLevel = serviceLevel.AI;
  
  constructor(public modalRef: BsModalRef) { }

  ngOnInit(): void {
  }

  
  enableQueryView() {
    this.isQueryViewEnabled = true;
    this.isApiViewEnabled = false;
    this.isFiltersViewEnabled = false;
  }

  enableApiView() {
    this.isQueryViewEnabled = false;
    this.isApiViewEnabled = true;
    this.isFiltersViewEnabled = false;
  }

  closePopUp() {
    this.modalRef.hide();
  }

  saveAPI(apiData) {
    this.Data.emit({source : CustomDataTypes.API,apiData : apiData});
    this.closePopUp();
  }

  // Save Query custom methods region starts.
  saveQuery(data) {
    this.Data.emit({source : CustomDataTypes.Query,query : data.query, DateColumn : data.DateColumn});
    this.closePopUp();
  }
  // Save Query custom methods region ends.
}
