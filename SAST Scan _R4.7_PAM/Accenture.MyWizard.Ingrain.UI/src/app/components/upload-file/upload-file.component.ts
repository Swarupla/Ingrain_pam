import { Component } from '@angular/core';
import { DialogConfig } from 'src/app/dialog/dialog-config';
import { DialogRef } from 'src/app/dialog/dialog-ref';

@Component({
  selector: 'app-upload-file',
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.scss']
})
export class UploadFileComponent {
  files = [];
  uploadStatus: string;
  connectFlag: boolean;

  // config: DialogConfig = incoming data,
  // dialog: DialogRef = to send outgoing Data kkk
  constructor(public config: DialogConfig,
    public dialog: DialogRef) { }

  onClose() {
    this.dialog.close();
  }

  getFileDetails(e) {
    this.files = [];
    for (let i = 0; i < e.target.files.length; i++) {
      this.files.push(e.target.files[i]);
    }
    this.dialog.close(this.files);
  }

  openModalForApi() {
    this.connectFlag = true;
    this.dialog.close(this.connectFlag);
  }


  allowDrop(event) {
    event.preventDefault();
  }

  onDrop(event) {
    event.preventDefault();

    for (let i = 0; i < event.dataTransfer.files.length; i++) {
      this.files.push(event.dataTransfer.files[i]);
    }

    this.dialog.close(this.files);
  }



}
