import { Component, OnInit, ViewChild } from '@angular/core';
import { DecryptService } from 'src/app/_services/decrypt.service';
import { NotificationService } from 'src/app/_services/notification-service.service';

@Component({
  selector: 'app-marketplace-file-encryption',
  templateUrl: './marketplace-file-encryption.component.html',
  styleUrls: ['./marketplace-file-encryption.component.css']
})
export class MarketplaceFileEncryptionComponent implements OnInit {

  files = [];
  @ViewChild("fileInput") selectFile;

  constructor(private _decryptService: DecryptService, private _notificationService: NotificationService) { }

  ngOnInit(): void {
  }

  submit(value) {
    const filePath =  value;
    const filePayload = { 'fileUpload': {} };
    const finalPayload = [];

    let params = {
      'filepath': filePath
    }

    filePayload.fileUpload['filepath'] = this.files;
    finalPayload.push(filePayload);
    this._decryptService.marketPlaceFileEncrypt(filePayload, params).subscribe(res => {
      if (res) {
        if (res === 'Success')
          this._notificationService.success('File Encryption done');
      } else {
        console.log(res);
      }
    }, error => {
      console.log(error);
    });
  }

  getFileDetails(e) {
    this.files = [];
    console.log(e.target.files.length);
    this.files = e.target.files;
  }

}
