import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-target-node',
  templateUrl: './target-node.component.html',
  styleUrls: ['./target-node.component.scss']
})
export class TargetNodeComponent implements OnInit {
  @Input() title: string;
  @Input()  NodeList = [];
  @Input() jsonTargetNode;
  @Output() Data = new EventEmitter<any>();
  selectedNode : string;
  enableNodeListView : boolean = false;

  constructor(public modalRef: BsModalRef, private ns: NotificationService) { }

  ngOnInit(): void {
    this.selectedNode = this.NodeList.length === 1 ? this.NodeList[0] : undefined;
  }

  onEnter() {
    if (this.selectedNode) {
      this.Data.emit(this.selectedNode);
      this.closePopUp();
    } else {
      this.ns.error('Please select one Node.');
    }
  }

  closePopUp() {
    this.modalRef.hide();
  }

}
