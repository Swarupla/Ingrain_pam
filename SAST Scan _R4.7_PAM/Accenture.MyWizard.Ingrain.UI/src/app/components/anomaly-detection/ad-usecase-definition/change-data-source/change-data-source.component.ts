import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-change-data-source',
  templateUrl: './change-data-source.component.html',
  styleUrls: ['./change-data-source.component.scss']
})
export class ChangeDataSourceComponent implements OnInit {
  @Output() outputData = new EventEmitter<any>();
  title : string = 'Change Data Source';

  constructor(public modalRef: BsModalRef) { }

  ngOnInit(): void {
  }

  onClose() {
    this.modalRef.hide();
  }

  getUploadedData(data){
    this.outputData.emit(data.finalPayload);
    this.onClose();
  }

}
