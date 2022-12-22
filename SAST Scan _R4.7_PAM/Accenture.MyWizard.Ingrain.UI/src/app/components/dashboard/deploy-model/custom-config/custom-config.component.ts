import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl } from '@angular/forms';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { ApiService } from 'src/app/_services/api.service';
import { AppUtilsService } from 'src/app/_services/app-utils.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-custom-config',
  templateUrl: './custom-config.component.html',
  styleUrls: ['./custom-config.component.scss']
})
export class CustomConfigComponent implements OnInit {
  @Input() title: string;
  @Input() dataSource: string;
  @Input() selectedService: string;
  @Input() serviceLevel : string;
  @Output() customConfig = new EventEmitter<any>();

  selectedConfigList = [];
  constraintsList = [];
  ConfigStorageKey = 'CustomConfiguration';
  dropdownOptions = new FormControl([]);
  defaultDisplayCount = 3;
  disableDropdown: boolean = false;
  isSaveDisabled : boolean = false;

  constructor(public modalRef: BsModalRef, private _apiService: ApiService, private notificationService: NotificationService,
    private appUtilsService: AppUtilsService) {
  }
  
  ngOnInit(): void {
    this.appUtilsService.loadingStarted();
    const selectedCustomConfig = sessionStorage.getItem(`${this.selectedService}${this.ConfigStorageKey}`) ? JSON.parse(sessionStorage.getItem(`${this.selectedService}${this.ConfigStorageKey}`)) :[];
    this._apiService.get('GetCustomConfiguration', { serviceType: this.selectedService, serviceLevel : this.serviceLevel }).subscribe(response => {
      this.constraintsList = response;
      this.isSaveDisabled = (this.constraintsList.length > 0) ? false : true;
      selectedCustomConfig?.forEach(constraint=>{
        this.constraintsList.forEach(element=>{
          if(element.ConstraintCode === constraint){
            this.selectedConfigList.push(element);
          }
        });
      });
      this.dropdownOptions.setValue(this.selectedConfigList);
      this.appUtilsService.loadingEnded();
    }, error => {
      this.appUtilsService.loadingEnded();
      this.constraintsList = [];
      this.disableDropdown = true;
      this.notificationService.error('Something Went Wrong while fetching Custom Constraints.');
    });
    
  }

  selectionChanged(data) {
    if (data.value.length === 1 && data.value[0] == 'Select All') {

    } else {
      if (data.value.length > 0) {
        this.selectedConfigList = this.removeDuplicateFromArray(data.value);
      } else {
        this.selectedConfigList = [];
      }
    }
  }

  unselectOption(name, index, event) {
    event.stopPropagation();
    let selectedOption = this.removeDuplicateFromArray(this.dropdownOptions.value);
    selectedOption.splice(index, 1);
    this.selectedConfigList = selectedOption;
    if (selectedOption.length === 0) {
      this.dropdownOptions.setValue([]);
    } else {
      this.dropdownOptions.setValue(selectedOption);
    }
    event.preventDefault();
  }

  removeDuplicateFromArray(data) {
    let filteredArray = new Set(data);
    return Array.from(filteredArray);
  }

  apply() {

    let finalList = [];
    let constraintNames = [];
    this.selectedConfigList.forEach(element => {
      finalList.push(element.ConstraintCode);
      constraintNames.push(element.ConstraintName);
    });

    this.customConfig.emit(finalList);
    this.closePopUp();
    if (this.selectedConfigList.length > 0) {
      this.notificationService.success('You have selected - ' + constraintNames + ' custom constraints for ' + this.selectedService);
    }
  }

  closePopUp() {
    this.modalRef.hide();
  }
}
