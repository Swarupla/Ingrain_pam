import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AdProblemStatementService } from 'src/app/_services/ad-problem-statement.service';
import { LocalStorageService } from 'src/app/_services/local-storage.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { ProblemStatementService } from 'src/app/_services/problem-statement.service';


@Component({
  selector: 'app-modal-name-dialog',
  templateUrl: './modal-name-dialog.component.html',
  styleUrls: ['./modal-name-dialog.component.scss']
})
export class ModalNameDialogComponent implements OnInit {
  @Input() title: string;
  @Input() placeholder : string;
  @Input() isAnomalyService : boolean;
  @Output() Data = new EventEmitter<any>();

  modalName: string;

  constructor(public modalRef: BsModalRef, private ns: NotificationService, private pb: ProblemStatementService,
    private adProblemStatementServive : AdProblemStatementService, private ls : LocalStorageService) { }
  
  ngOnInit(): void {
  }

  onEnter() {
    if (this.modalName?.trim()) {
      if(this.isAnomalyService == true){
        this.adProblemStatementServive.getExistingModelNameAD(this.modalName.trim()).subscribe(message => {
          if (message === false) {
            this.setModelNameinLocalStorage(this.modalName);
            this.Data.emit(this.modalName);
            this.closePopUp();
          } else {
            message = 'The model name already exists. Choose a different name.';
            this.ns.error(message);
          }
        }, error => {
          this.closePopUp();
          this.ns.error(error.error);
        });
      }else{
      this.pb.getExistingModelName(this.modalName.trim()).subscribe(message => {
        if (message === false) {
          this.Data.emit(this.modalName);
          this.closePopUp();
        } else {
          message = 'The model name already exists. Choose a different name.';
          this.ns.error(message);
        }
      }, error => {
        this.closePopUp();
        this.ns.error('Something went wrong.');
      });
    }
    } else {
      this.ns.error('model name can not be empty');
    }
  }

  closePopUp() {
    this.modalRef.hide();
  }

  setModelNameinLocalStorage(modelName) {
    this.ls.setLocalStorageData('modelName', modelName);
  }
}
