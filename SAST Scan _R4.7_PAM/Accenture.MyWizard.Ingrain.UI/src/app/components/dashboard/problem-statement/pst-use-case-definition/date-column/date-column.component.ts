import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-date-column',
  templateUrl: './date-column.component.html',
  styleUrls: ['./date-column.component.scss']
})
export class DateColumnComponent implements OnInit {
  @Input() title: string;
  @Input()  dateColumnsList = [];
  @Output() Data = new EventEmitter<any>();

  constructor(public modalRef: BsModalRef, private ns: NotificationService) { }
  selectedColumn: string;

  ngOnInit(): void {
    this.selectedColumn = this.dateColumnsList.length === 1 ? this.dateColumnsList[0] : undefined;
  }

  onEnter() {
    if (this.selectedColumn) {
      this.Data.emit(this.selectedColumn);
      this.closePopUp();
    } else {
      this.ns.error('Please select at least one Date Column.');
    }
  }

  closePopUp() {
    this.modalRef.hide();
  }

}
