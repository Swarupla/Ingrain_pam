import { Component, OnInit, TemplateRef } from '@angular/core';
import { DialogRef } from 'src/app/dialog/dialog-ref';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { DatePipe } from '@angular/common';
import * as jspdf from 'jspdf';
import html2canvas from 'html2canvas';
import { DeployModelService } from 'src/app/_services/deploy-model.service';
import { NotificationService } from 'src/app/_services/notification-service.service';
import { DialogConfig } from 'src/app/dialog/dialog-config';

@Component({
  selector: 'app-completion-certificate',
  templateUrl: './completion-certificate.component.html',
  styleUrls: ['./completion-certificate.component.css']
})
export class CompletionCertificateComponent implements OnInit {

  userData = {
    username: '',
    email: ''
  };

  email: string;
  fullName: string;
  sysDate;

  modalRef: BsModalRef;
  config = {
    backdrop: true,
    ignoreBackdropClick: true,
    class: 'training-certificatioin',
  };
  isCertificate: boolean;
  model: any = {};
  // @ViewChild('certificationTemplate1') certificationTemplate1: ElementRef;

  constructor(private dr: DialogRef, private _modalService: BsModalService,
    private datePipe: DatePipe, private _deployModelService: DeployModelService, private _notificationService: NotificationService,
    private dialogConfig: DialogConfig, ) { }

  ngOnInit() {
    const today = new Date();
    this.sysDate = this.formatDate(today);
    this.isCertificate = this.dialogConfig.data.configuredCertificationFlag;
  }

  formatDate(date) {
    return this.datePipe.transform(date, 'd MMMM, y');
  }

  onClose() {
    this.dr.close();
  }

  onEnter(Name, Email) {
    this.fullName = Name.model;
    this._deployModelService.certificateForWorkshop(this.fullName, Email.model).subscribe(data => {
      console.log(data);
      if (data) {
        if (data === 'Success') {
          this._notificationService.success('Email has been sent to you with virtual certificate');
        }
        this.onClose();
      }
    }, error => {
    });
  }

  viewCertificate(certificateTemp: TemplateRef<any>, fullName) {
    this.fullName = fullName.model;
    this.modalRef = this._modalService.show(certificateTemp, this.config);
  }

  ValidateEmail(inputText) {
    const mailformat = '/^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/';
    if (inputText.value.match(mailformat)) {
      return true;
    } else {
      alert('You have entered an invalid email address!');
      return false;
    }
  }

  download() {
    const data = document.getElementById('certificationTemplate');
    html2canvas(data).then(canvas => {
      const contentDataURL = canvas.toDataURL('image/png');
      const pdf = new jspdf.jsPDF('l', 'cm', 'a4'); // Generates PDF in landscape mode
      // let pdf = new jspdf('p', 'cm', 'a4'); Generates PDF in portrait mode
      pdf.addImage(contentDataURL, 'PNG', 0, 0, 29.7, 21.0);
      pdf.save('Completion Certificate.pdf');
    });    
  }
}
